// Compat.cs: might not be ideal but it eases code sharing with existing code during the initial implementation.
using System;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;

using Xamarin.Bundler;
using Xamarin.Linker;
using Xamarin.Utils;

using System;
using Mono.Cecil;
using Mono.Tuner;
using Xamarin.Bundler;

namespace Xamarin.Linker {

	public static class Profile {
		public static bool IsProductAssembly (AssemblyDefinition assembly)
		{
			return assembly.Name.Name == LinkerConfiguration.Instance.PlatformAssembly;
		}

		public static bool IsProductAssembly (string assembly)
		{
			return assembly == LinkerConfiguration.Instance.PlatformAssembly;
		}
	}

	public class App {
		public Optimizations Optimizations { get; } = new Optimizations () {
			CustomAttributesRemoval = true,
		};
	}


	// from StaticRegistrar.cs
	class RegisterAttribute : Attribute {
		public RegisterAttribute () {}
		public RegisterAttribute (string name) {
			this.Name = name;
		}

		public RegisterAttribute (string name, bool isWrapper) {
			this.Name = name;
			this.IsWrapper = isWrapper;
		}

		public string Name { get; set; }
		public bool IsWrapper { get; set; }
		public bool SkipRegistration { get; set; }

		// FIXME: does not include attributes removed by the linker
		static public bool TryGetAttribute (ICustomAttributeProvider provider, string @namespace, string attributeName, out ICustomAttribute attribute)
		{
			attribute = null;
			if (provider.HasCustomAttributes) {
				foreach (var custom_attribute in provider.CustomAttributes) {
					if (!custom_attribute.AttributeType.Is (@namespace, attributeName))
						continue;
					attribute = custom_attribute;
					return true;
				}
			}
			return false;
		}

		static public RegisterAttribute GetRegisterAttribute (TypeReference type)
		{
			RegisterAttribute rv = null;

			if (!TryGetAttribute (type.Resolve (), "Foundation", "RegisterAttribute", out var attrib))
				return null;

			if (!attrib.HasConstructorArguments) {
				rv = new RegisterAttribute ();
			} else {
				switch (attrib.ConstructorArguments.Count) {
				case 0:
					rv = new RegisterAttribute ();
					break;
				case 1:
					rv = new RegisterAttribute ((string) attrib.ConstructorArguments [0].Value);
					break;
				case 2:
					rv = new RegisterAttribute ((string) attrib.ConstructorArguments [0].Value, (bool) attrib.ConstructorArguments [1].Value);
					break;
				default:
					throw ErrorHelper.CreateError (4124, type.FullName);
				}
			}

			if (attrib.HasProperties) {
				foreach (var prop in attrib.Properties) {
					switch (prop.Name) {
					case "IsWrapper":
						rv.IsWrapper = (bool) prop.Argument.Value;
						break;
					case "Name":
						rv.Name = (string) prop.Argument.Value;
						break;
					case "SkipRegistration":
						rv.SkipRegistration = (bool) prop.Argument.Value;
						break;
					default:
						throw ErrorHelper.CreateError (4124, type.FullName + " " + prop.Name);
					}
				}
			}

			return rv;
		}
	}
}

namespace Xamarin.Bundler {
	public partial class Application {
		public LinkerConfiguration Configuration { get; private set; }

		public Application (LinkerConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		public string ProductName {
			get {
				switch (Platform) {
				case ApplePlatform.iOS:
					return "Microsoft.iOS";
				case ApplePlatform.TVOS:
					return "Microsoft.tvOS";
				case ApplePlatform.WatchOS:
					return "Microsoft.watchOS";
				case ApplePlatform.MacOSX:
					return "Microsoft.macOS";
				default:
					throw ErrorHelper.CreateError (177, Errors.MX0177 /* "Unknown platform: {0}. This usually indicates a bug; please file a bug report at https://github.com/xamarin/xamarin-macios/issues/new with a test case." */, Platform);
				}
			}
		}

		public void SelectRegistrar ()
		{
			if (Registrar == RegistrarMode.Default) {
				if (LinkMode == LinkMode.None && IsDefaultMarshalManagedExceptionMode) {
					Registrar = RegistrarMode.PartialStatic;
				} else {
					Registrar = RegistrarMode.Dynamic;
				}
			}
			Driver.Log (1, $"Registrar mode: {Registrar}");
		}

		public AssemblyBuildTarget LibMonoLinkMode {
			get { throw new NotImplementedException (); }
		}

		public AssemblyBuildTarget LibXamarinLinkMode {
			get { throw new NotImplementedException (); }
		}

		public bool HasAnyDynamicLibraries {
			get { throw new NotImplementedException (); }
		}

		public string GetLibMono (AssemblyBuildTarget build_target)
		{
			throw new NotImplementedException ();
		}

		public string GetLibXamarin (AssemblyBuildTarget build_target)
		{
			throw new NotImplementedException ();
		}
	}

	public partial class Driver {
		public static string NAME {
			get { return "xamarin-bundler"; }
		}

		public static string GetArch32Directory (Application app)
		{
			throw new NotImplementedException ();
		}

		public static string GetArch64Directory (Application app)
		{
			throw new NotImplementedException ();
		}
	}

	// We can't make the linker use a LinkerContext subclass (DerivedLinkerContext), so we make DerivedLinkerContext
	// derive from this class, and then we redirect to the LinkerContext instance here.
	public class DotNetLinkContext {
		public LinkerConfiguration LinkerConfiguration;

		public AssemblyAction UserAction {
			get { throw new NotImplementedException (); }
			set { throw new NotImplementedException (); }
		}

		public AnnotationStore Annotations {
			get {
				return LinkerConfiguration.Context.Annotations;
			}
		}

		public AssemblyDefinition GetAssembly (string name)
		{
			return LinkerConfiguration.Context.GetLoadedAssembly (name);
		}
	}

	public class Pipeline {

	}
}

namespace Xamarin.Linker {
	public class BaseProfile : Profile {
		public BaseProfile (LinkerConfiguration config)
			: base (config)
		{
		}
	}

	public class Profile {
		public LinkerConfiguration Configuration { get; private set; }

		public Profile (LinkerConfiguration config)
		{
			Configuration = config;
		}

		public Profile Current {
			get { return this; }
		}

		public string ProductAssembly {
			get { return Configuration.PlatformAssembly; }
		}

		public bool IsProductAssembly (AssemblyDefinition assembly)
		{
			return assembly.Name.Name == Configuration.PlatformAssembly;
		}

		public bool IsSdkAssembly (AssemblyDefinition assembly)
		{
			return Configuration.FrameworkAssemblies.Contains (Assembly.GetIdentity (assembly));
		}
	}
}

namespace Mono.Linker {
	public static class LinkContextExtensions {
		public static void LogMessage (this LinkContext context, string messsage)
		{
			throw new NotImplementedException ();
		}
		public static IEnumerable<AssemblyDefinition> GetAssemblies (this LinkContext context)
		{
			return LinkerConfiguration.GetInstance (context).Assemblies;
		}
		public static Dictionary<IMetadataTokenProvider, object> GetCustomAnnotations (this AnnotationStore self, string name)
		{
			throw new NotImplementedException ();
		}
	}
}
