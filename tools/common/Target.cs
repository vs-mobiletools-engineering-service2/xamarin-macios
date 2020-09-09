// Copyright 2013--2014 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


using Mono.Cecil;
using Mono.Tuner;
using Mono.Linker;
using Xamarin.Linker;

using Xamarin.Utils;
using Registrar;
using ObjCRuntime;

#if MONOTOUCH
using MonoTouch;
using MonoTouch.Tuner;
using PlatformResolver = MonoTouch.Tuner.MonoTouchResolver;
using PlatformLinkContext = MonoTouch.Tuner.MonoTouchLinkContext;
#elif MMP
using MonoMac.Tuner;
using PlatformResolver = Xamarin.Bundler.MonoMacResolver;
using PlatformLinkContext = MonoMac.Tuner.MonoMacLinkContext;
#elif NET
using LinkerOptions = Xamarin.Linker.LinkerConfiguration;
using PlatformLinkContext = Xamarin.Tuner.DerivedLinkContext;
using PlatformResolver = Xamarin.Linker.DotNetResolver;
#else
#error Invalid defines
#endif

namespace Xamarin.Bundler {
	public partial class Target {
		public Application App;
		public AssemblyCollection Assemblies = new AssemblyCollection (); // The root assembly is not in this list.

		public PlatformLinkContext LinkContext;
		public LinkerOptions LinkerOptions;
		public PlatformResolver Resolver = new PlatformResolver ();

		public HashSet<string> Frameworks = new HashSet<string> ();
		public HashSet<string> WeakFrameworks = new HashSet<string> ();

		internal StaticRegistrar StaticRegistrar { get; set; }

		// If we didn't link because the existing (cached) assemblyes are up-to-date.
		bool cached_link = false;

		Symbols dynamic_symbols;

		// Note that each 'Target' can have multiple abis: armv7+armv7s for instance.
		public List<Abi> Abis;

		// If we're targetting a 32 bit arch for this target.
		bool? is32bits;
		public bool Is32Build {
			get {
				if (!is32bits.HasValue)
					is32bits = Application.IsArchEnabled (Abis, Abi.Arch32Mask);
				return is32bits.Value;
			}
		}

		// If we're targetting a 64 bit arch for this target.
		bool? is64bits;
		public bool Is64Build {
			get {
				if (!is64bits.HasValue)
					is64bits = Application.IsArchEnabled (Abis, Abi.Arch64Mask);
				return is64bits.Value;
			}
		}

		public Target (Application app)
		{
			this.App = app;
			this.StaticRegistrar = new StaticRegistrar (this);
		}

		public Assembly AddAssembly (AssemblyDefinition assembly)
		{
			var asm = new Assembly (this, assembly);
			Assemblies.Add (asm);
			return asm;
		}

		// This will find the link context, possibly looking in container targets.
		public PlatformLinkContext GetLinkContext ()
		{
			if (LinkContext != null)
				return LinkContext;
#if MTOUCH
			if (App.IsExtension && App.IsCodeShared)
				return ContainerTarget.GetLinkContext ();
#endif
			return null;
		}

		public bool CachedLink {
			get {
				return cached_link;
			}
		}

		public void ExtractNativeLinkInfo (List<Exception> exceptions)
		{
			foreach (var a in Assemblies) {
				try {
					a.ExtractNativeLinkInfo ();
				} catch (Exception e) {
					exceptions.Add (e);
				}
			}

#if MTOUCH
			if (!App.OnlyStaticLibraries && Assemblies.Count ((v) => v.HasLinkWithAttributes) > 1) {
				ErrorHelper.Warning (127, Errors.MT0127);
				App.ClearAssemblyBuildTargets (); // the default is to compile to static libraries, so just revert to the default.
			}
#endif
		}

		[DllImport (Constants.libSystemLibrary, SetLastError = true)]
		static extern string realpath (string path, IntPtr zero);

		public static string GetRealPath (string path)
		{
			// For some reason realpath doesn't always like filenames only, and will randomly fail.
			// Prepend the current directory if there's no directory specified.
			if (string.IsNullOrEmpty (Path.GetDirectoryName (path)))
				path = Path.Combine (Environment.CurrentDirectory, path);

			var rv = realpath (path, IntPtr.Zero);
			if (rv != null)
				return rv;

			var errno = Marshal.GetLastWin32Error ();
			ErrorHelper.Warning (54, Errors.MT0054, path, FileCopier.strerror (errno), errno);
			return path;
		}

		public void ValidateAssembliesBeforeLink ()
		{
			if (App.LinkMode != LinkMode.None) {
				foreach (Assembly assembly in Assemblies) {
					if ((assembly.AssemblyDefinition.MainModule.Attributes & ModuleAttributes.ILOnly) == 0)
						throw ErrorHelper.CreateError (2014, Errors.MT2014, assembly.AssemblyDefinition.MainModule.FileName);
				}
			}
		}

		public void ComputeLinkerFlags ()
		{
			ComputePInvokeLinkerFlags ();
			foreach (var a in Assemblies)
				a.ComputeLinkerFlags ();
		}

		void ComputePInvokeLinkerFlags ()
		{
#if MTOUCH
			if (!Driver.IsDotNet)
				return;

			// Check for native libraries from the BCL
			var symbols = GetAllSymbols ();
			var native_libraries = new Dictionary<string, List<Tuple<Symbol, Mono.Cecil.MethodDefinition>>> ();
			foreach (var symbol in symbols) {
				foreach (var member in symbol.Members) {
					var md = member.Resolve () as Mono.Cecil.MethodDefinition;
					if (md == null)
						continue;
					var pinvoke = md.PInvokeInfo;
					if (!native_libraries.TryGetValue (pinvoke.Module.Name, out var list))
						native_libraries [pinvoke.Module.Name] = list = new List<Tuple<Symbol, Mono.Cecil.MethodDefinition>> ();
					list.Add (new Tuple<Symbol, MethodDefinition> (symbol, md));

				}
			}

			string lib_extension;
			var mode = App.LibMonoLinkMode;
			switch (mode) {
			case AssemblyBuildTarget.DynamicLibrary:
				lib_extension = ".dylib";
				break;
			case AssemblyBuildTarget.Framework:
			case AssemblyBuildTarget.StaticObject:
				lib_extension = ".a";
				break;
			default:
				throw ErrorHelper.CreateError (100, Errors.MT0100, mode);
			}

			var bcl_implementation_dir = Driver.GetFrameworkDirectory (this.App);
			foreach (var nl in native_libraries) {
				var lib = nl.Key;

				if (lib == "__Internal")
					continue;
				else if (Path.IsPathRooted (lib))
					continue; // Reference to a system library

				if (!lib.StartsWith ("lib", StringComparison.Ordinal))
					lib = "lib" + lib;

				var lib_path = Path.Combine (bcl_implementation_dir, lib + lib_extension);
				if (!File.Exists (lib_path)) {
					// FIXME: Add an actual warning
					Driver.Log ("Could not find the native library {0}. This library is referenced from {1} P/Invokes:", lib_path, nl.Value.Count);
					foreach (var (symbol, md) in nl.Value) {
						Driver.Log ("    {0} in {1}", symbol.Name, md.FullName);
						App.IgnoredSymbols.Add (symbol.Name);
						AddAssemblyWithInexistentPInvokes (md.Module.Assembly);
					}
					continue;
				}

				var nm_output = new StringBuilder ();
				var rv = Driver.RunCommand ("nm", new string [] { "-jg", lib_path }, output: nm_output);
				if (rv != 0) {
					Driver.Log ("Could not list symbols in {0}, nm failed to execute (exit code: {1}):", lib_path, rv);
					Driver.Log ($"nm -jg {lib_path}");
					Driver.Log (nm_output.ToString ());
				} else {
					var actual_symbols = new HashSet<string> (nm_output.ToString ().Split (new char [] { '\n' }, StringSplitOptions.RemoveEmptyEntries));
					var not_found = nl.Value.Where ((v) => !actual_symbols.Contains ("_" + v.Item1.Name));
					if (not_found.Any ()) {
						not_found = not_found.Distinct (new III ());
						not_found = not_found.ToArray ();
						Driver.Log ("There are {0} P/Invokes that reference functions supposedly in {1}:", not_found.Count (), lib_path);
						foreach (var (symbol, md) in not_found) {
							Driver.Log ("    {0} in {1}", symbol.Name, md.FullName);
							App.IgnoredSymbols.Add (symbol.Name);
							AddAssemblyWithInexistentPInvokes (md.Module.Assembly);
						}
					}
				}

				foreach (var abi in Abis)
					linker_flags_by_abi [abi].AddLinkWith (lib_path);
			}
#endif
		}

		class III : IEqualityComparer<Tuple<Symbol, Mono.Cecil.MethodDefinition>>
		{
			public bool Equals (Tuple<Symbol, MethodDefinition> x, Tuple<Symbol, MethodDefinition> y)
			{
				return x.Item1.Name == y.Item1.Name && x.Item2.FullName == y.Item2.FullName;
			}

			public int GetHashCode (Tuple<Symbol, MethodDefinition> obj)
			{
				return obj.Item1.Name.GetHashCode () ^ obj.Item2.FullName.GetHashCode ();
			}
		}

		public void GatherFrameworks ()
		{
			Assembly asm = null;

			foreach (var assembly in Assemblies) {
				if (assembly.AssemblyDefinition.Name.Name == Driver.GetProductAssembly (App)) {
					asm = assembly;
					break;
				}
			}

			if (asm == null)
				throw ErrorHelper.CreateError (99, Errors.MX0099, $"could not find the product assembly {Driver.GetProductAssembly(App)} in the list of assemblies referenced by the executable");

			AssemblyDefinition productAssembly = asm.AssemblyDefinition;

			// *** make sure any change in the above lists (or new list) are also reflected in 
			// *** Makefile so simlauncher-sgen does not miss any framework

			HashSet<string> processed = new HashSet<string> ();
#if !MONOMAC
			Version v80 = new Version (8, 0);
#endif

			foreach (ModuleDefinition md in productAssembly.Modules) {
				foreach (TypeDefinition td in md.Types) {
					// process only once each namespace (as we keep adding logic below)
					string nspace = td.Namespace;
					if (processed.Contains (nspace))
						continue;
					processed.Add (nspace);

					Framework framework;
					if (Driver.GetFrameworks (App).TryGetValue (nspace, out framework)) {
						// framework specific processing
						switch (framework.Name) {
#if MONOMAC
						case "QTKit":
							// we already warn in Frameworks.cs Gather method
							if (!Driver.LinkProhibitedFrameworks)
								continue;
							break;
#else
						case "CoreAudioKit":
							// CoreAudioKit seems to be functional in the iOS 9 simulator.
							if (App.IsSimulatorBuild && App.SdkVersion.Major < 9)
								continue;
							break;
						case "Metal":
						case "MetalKit":
						case "MetalPerformanceShaders":
						case "CoreNFC":
							// some frameworks do not exists on simulators and will result in linker errors if we include them
							if (App.IsSimulatorBuild)
								continue;
							break;
						case "DeviceCheck":
							if (App.IsSimulatorBuild && App.SdkVersion.Major < 13)
								continue;
							break;
						case "PushKit":
							// in Xcode 6 beta 7 this became an (ld) error - it was a warning earlier :(
							// ld: embedded dylibs/frameworks are only supported on iOS 8.0 and later (@rpath/PushKit.framework/PushKit) for architecture armv7
							// this was fixed in Xcode 6.2 (6.1 was still buggy) see #29786
							if ((App.DeploymentTarget < v80) && (Driver.XcodeVersion < new Version (6, 2))) {
								ErrorHelper.Warning (49, Errors.MT0049, framework.Name);
								continue;
							}
							break;
						case "WatchKit":
							// Xcode 11 doesn't ship WatchKit for iOS
							if (Driver.XcodeVersion.Major == 11 && App.Platform == ApplePlatform.iOS) {
								ErrorHelper.Warning (5219, Errors.MT5219);
								continue;
							}
							break;
#endif
						}

						if (App.SdkVersion >= framework.Version) {
							var add_to = framework.AlwaysWeakLinked || App.DeploymentTarget < framework.Version ? asm.WeakFrameworks : asm.Frameworks;
							add_to.Add (framework.Name);
							continue;
						} else {
							Driver.Log (3, "Not linking with the framework {0} (used by the type {1}) because it was introduced in {2} {3}, and we're using the {2} {4} SDK.", framework.Name, td.FullName, App.PlatformName, framework.Version, App.SdkVersion);
						}
					}
				}
			}

			// Make sure there are no duplicates between frameworks and weak frameworks.
			// Keep the weak ones.
			asm.Frameworks.ExceptWith (asm.WeakFrameworks);
		}

		internal static void PrintAssemblyReferences (AssemblyDefinition assembly)
		{
			if (Driver.Verbosity < 2)
				return;

			var main = assembly.MainModule;
			Driver.Log ($"Loaded assembly '{assembly.FullName}' from {StringUtils.Quote (assembly.MainModule.FileName)}");
			foreach (var ar in main.AssemblyReferences)
				Driver.Log ($"    References: '{ar.FullName}'");
		}

		public Symbols GetAllSymbols ()
		{
			CollectAllSymbols ();
			return dynamic_symbols;
		}

		HashSet<string> assemblies_with_inexistent_pinvokes = new HashSet<string> ();
		public void AddAssemblyWithInexistentPInvokes (AssemblyDefinition ad)
		{
			assemblies_with_inexistent_pinvokes.Add (ad.Name.Name);
		}

		public HashSet<string> AssembliesWithInexistentPInvokes {
			get {
				return assemblies_with_inexistent_pinvokes;
			}
		}

		public void CollectAllSymbols ()
		{
			if (dynamic_symbols != null)
				return;

			var cache_location = Path.Combine (App.Cache.Location, "entry-points.txt");
			if (cached_link) {
				dynamic_symbols = new Symbols ();
				dynamic_symbols.Load (cache_location, this);
			} else {
				if (LinkContext == null) {
					// This happens when using the simlauncher and the msbuild tasks asked for a list
					// of symbols (--symbollist). In that case just produce an empty list, since the
					// binary shouldn't end up stripped anyway.
					dynamic_symbols = new Symbols ();
				} else {
					dynamic_symbols = LinkContext.RequiredSymbols;
				}

				// keep the debugging helper in debugging binaries only
				var has_mono_pmip = App.EnableDebug;
#if MMP
				has_mono_pmip &= !Driver.IsUnifiedFullSystemFramework;
#endif
				if (has_mono_pmip)
					dynamic_symbols.AddFunction ("mono_pmip");

				bool has_dyn_msgSend;
#if MONOTOUCH
				has_dyn_msgSend = App.IsSimulatorBuild;
#else
				has_dyn_msgSend = App.MarshalObjectiveCExceptions != MarshalObjectiveCExceptionMode.Disable && !App.RequiresPInvokeWrappers && Is64Build;
#endif

				if (has_dyn_msgSend) {
					dynamic_symbols.AddFunction ("xamarin_dyn_objc_msgSend");
					dynamic_symbols.AddFunction ("xamarin_dyn_objc_msgSendSuper");
					dynamic_symbols.AddFunction ("xamarin_dyn_objc_msgSend_stret");
					dynamic_symbols.AddFunction ("xamarin_dyn_objc_msgSendSuper_stret");
				}

#if MONOTOUCH
				if (App.EnableProfiling && App.LibProfilerLinkMode == AssemblyBuildTarget.StaticObject)
					dynamic_symbols.AddFunction ("mono_profiler_init_log");
#endif

				dynamic_symbols.Save (cache_location);
			}

			// Warn if we're asked to ignore symbols that don't exist.
			foreach (var name in App.IgnoredSymbols.Where ((v) => dynamic_symbols.Contains (v)))
				ErrorHelper.Warning (5218, Errors.MT5218, StringUtils.Quote (name));
		}

		bool IsRequiredSymbol (Symbol symbol, Assembly single_assembly = null)
		{
			if (!symbol.Ignore.HasValue)
				symbol.Ignore = App.IgnoredSymbols.Contains (symbol.Name);
			if (symbol.Ignore == true)
				return false;

			// Check if this symbol is used in the assembly we're filtering to
			if (single_assembly != null && !symbol.Members.Any ((v) => v.Module.Assembly == single_assembly.AssemblyDefinition))
				return false; // nope, this symbol is not used in the assembly we're using as filter.

#if MTOUCH
			// If we're code-sharing, the managed linker might have found symbols
			// that are not in any of the assemblies in the current app.
			// This occurs because the managed linker processes all the
			// assemblies for all the apps together, but when linking natively
			// we're only linking with the assemblies that actually go into the app.
			if (App.IsCodeShared && symbol.Assemblies.Count > 0) {
				// So if this is a symbol related to any assembly, make sure
				// at least one of those assemblies are in the current app.
				if (!symbol.Assemblies.Any ((v) => Assemblies.Contains (v)))
					return false;
			}
#endif

			switch (symbol.Type) {
			case SymbolType.Field:
				return true;
			case SymbolType.Function:
#if MTOUCH
				// functions are not required if they're used in an assembly which isn't using dlsym, and we're AOT-compiling.
				if (App.IsSimulatorBuild)
					return true;
				if (single_assembly != null)
					return App.UseDlsym (single_assembly.FileName);

				if (symbol.Members?.Any () == true) {
					foreach (var member in symbol.Members) {
						if (App.UseDlsym (member.Module.FileName)) {
							// If any assembly uses dlsym to reference this symbol, it's a required symbol that must be preserved,
							// because otherwise stripping the binary will cause the symbol (but not the function itself) to be removed,
							// preventing any assembly using dlsym to find it.
							return true;
						}
					}
					// None of the members use dlsym (and we have at least one member), then we don't need to preserve the symbol.
					return false;
				}
#endif
				return true;
			case SymbolType.ObjectiveCClass:
				// Objective-C classes are not required when we're using the static registrar and we're not compiling to shared libraries,
				// (because the registrar code is linked into the main app, but not each shared library, 
				// so the registrar code won't keep symbols in the shared libraries).
				if (single_assembly != null)
					return true;
				return App.Registrar != RegistrarMode.Static;
			default:
				throw ErrorHelper.CreateError (99, Errors.MX0099, $"invalid symbol type {symbol.Type} for symbol {symbol.Name}");
			}
		}

		public Symbols GetRequiredSymbols (Assembly assembly = null)
		{
			CollectAllSymbols ();

			Symbols filtered = new Symbols ();
			foreach (var ep in dynamic_symbols) {
				if (IsRequiredSymbol (ep, assembly)) {
					filtered.Add (ep);
				}
			}
			return filtered ?? dynamic_symbols;
		}

#if MTOUCH
		IEnumerable<CompileTask> GenerateReferencingSource (string reference_m, IEnumerable<Symbol> symbols)
#else
		internal string GenerateReferencingSource (string reference_m, IEnumerable<Symbol> symbols)
#endif
		{
			if (!symbols.Any ()) {
				if (File.Exists (reference_m))
					File.Delete (reference_m);
#if MTOUCH
				yield break;
#else
				return null;
#endif
			}
			var sb = new StringBuilder ();
			sb.AppendLine ("#import <Foundation/Foundation.h>");
			foreach (var symbol in symbols) {
				switch (symbol.Type) {
				case SymbolType.Function:
				case SymbolType.Field:
					sb.Append ("extern void * ").Append (symbol.Name).AppendLine (";");
					break;
				case SymbolType.ObjectiveCClass:
					sb.AppendLine ($"@interface {symbol.ObjectiveCName} : NSObject @end");
					break;
				default:
					throw ErrorHelper.CreateError (99, Errors.MX0099, $"invalid symbol type {symbol.Type} for symbol {symbol.Name}");
				}
			}
			sb.AppendLine ("static void __xamarin_symbol_referencer () __attribute__ ((used)) __attribute__ ((optnone));");
			sb.AppendLine ("void __xamarin_symbol_referencer ()");
			sb.AppendLine ("{");
			sb.AppendLine ("\tvoid *value;");
			foreach (var symbol in symbols) {
				switch (symbol.Type) {
				case SymbolType.Function:
				case SymbolType.Field:
					sb.AppendLine ($"\tvalue = {symbol.Name};");
					break;
				case SymbolType.ObjectiveCClass:
					sb.AppendLine ($"\tvalue = [{symbol.ObjectiveCName} class];");
					break;
				default:
					throw ErrorHelper.CreateError (99, Errors.MX0099, $"invalid symbol type {symbol.Type} for symbol {symbol.Name}");
				}
			}
			sb.AppendLine ("}");
			sb.AppendLine ();

			Driver.WriteIfDifferent (reference_m, sb.ToString (), true);

#if MTOUCH
			foreach (var abi in GetArchitectures (AssemblyBuildTarget.StaticObject)) {
				var arch = abi.AsArchString ();
				var reference_o = Path.Combine (Path.GetDirectoryName (reference_m), arch, Path.GetFileNameWithoutExtension (reference_m) + ".o");
				var compile_task = new CompileTask {
					Target = this,
					Abi = abi,
					InputFile = reference_m,
					OutputFile = reference_o,
					SharedLibrary = false,
					Language = "objective-c",
				};
				yield return compile_task;
			}
#else
			return reference_m;
#endif
		}

		// This is to load the symbols for all assemblies, so that we can give better error messages
		// (with file name / line number information).
		public void LoadSymbols ()
		{
			foreach (var a in Assemblies)
				a.LoadSymbols ();
		}

	}
}
