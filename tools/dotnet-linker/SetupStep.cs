using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;
using MonoTouch.Tuner;
using Xamarin.Bundler;
using Xamarin.Linker;
using Xamarin.Linker.Steps;
using Xamarin.Tuner;

namespace Xamarin {

	public class ConfigurationAwareStep : BaseStep {
		public LinkerConfiguration Configuration {
			get { return LinkerConfiguration.Instance; }
		}
	}
	
	public class SetupStep : ConfigurationAwareStep {

		List<IStep> _steps;

		List<IStep> Steps {
			get {
				if (_steps == null) {
					var pipeline = typeof (LinkContext).GetProperty ("Pipeline").GetGetMethod ().Invoke (Context, null);
					_steps = (List<IStep>) pipeline.GetType ().GetField ("_steps", BindingFlags.Instance | BindingFlags.NonPublic).GetValue (pipeline);
				}
				return _steps;
			}
		}

		void InsertAfter (IStep step, string stepName)
		{
			for (int i = 0; i < Steps.Count;) {
				if (Steps [i++].GetType ().Name == stepName) {
					Steps.Insert (i, step);
					return;
				}
			}
			throw new InvalidOperationException ($"Could not insert {step} after {stepName}.");
		}

		void RemoveStep (string stepName)
		{
			for (int i = Steps.Count - 1; i >= 0; i--) {
				if (Steps [i].GetType ().Name == stepName) {
					Steps.RemoveAt (i);
					return;
				}
			}
		}

		protected override void Process ()
		{
			DerivedLinkContext.Instance.Context = Context;

			// partially implemented upstream - but we need a subset of it to support
			// [assembly:LinkerSafe] and [assembly:Preserve] attributes in user code
			InsertAfter (new CustomizeActions (), "LoadReferencesStep");

			if (LinkerConfiguration.Instance.WarnOnTypeRef.Count > 0) {
				InsertAfter (new PreLinkScanTypeReferenceStep (), "LoadReferencesStep");
				InsertAfter (new PostLinkScanTypeReferenceStep (), "OutputStep");
			}

			// we need to store the Field attribute in annotations, since it may end up removed.
			InsertAfter (new ProcessExportedFields (), "TypeMapStep");

			var prelink_subs = new MobileSubStepDispatcher ();
			prelink_subs.Add (new RemoveUserResourcesSubStep ());
			InsertAfter (prelink_subs, "RemoveSecurityStep");

			// https://github.com/mono/linker/issues/1045
			// InsertAfter (new RemoveBitcodeIncompatibleCodeStep (), "MarkStep");

			switch (Configuration.LinkMode) {
			case LinkMode.None:
				// remove about everything if we're not linking
				for (int i = Steps.Count - 1; i >= 0; i--) {
					switch (Steps [i].GetType ().Name) {
					// remove well-known steps as removing all but needed steps would make --custom-step unusable :|
					case "BlacklistStep":
					case "PreserveDependencyLookupStep":
					case "TypeMapStep":
					case "BodySubstituterStep":
					case "RemoveSecurityStep":
					case "RemoveUnreachableBlocksStep":
					case "MarkStep":
					case "SweepStep":
					case "CodeRewriterStep":
					case "CleanStep":
					case "RegenerateGuidStep":
					case "ClearInitLocalsStep":
					case "SealerStep":
						Steps.RemoveAt (i);
						break;
					}
				}
				// Mark and Sweep steps are where we normally update CopyUsed to Copy
				// so we need something else, more lightweight, to do this job
				InsertAfter (new DoNotLinkStep (), "LoadReferencesStep");
				break;
			case LinkMode.SDKOnly:
				// FIXME: it's so noisy that it makes the log viewer hang :(
				RemoveStep ("RemoveUnreachableBlocksStep");

				// platform assemblies (and friends) are linked along with the BCL
				InsertAfter (new LinkSdkStep (), "LoadReferencesStep");

				InsertAfter (new CoreTypeMapStep (), "TypeMapStep");

				// only our _old_ [Preserve] code is needed, other stuff is handled differently
				prelink_subs.Add (new ApplyPreserveAttribute ());

				// subs.Add (new OptimizeGeneratedCodeSubStep ());
				prelink_subs.Add (new RemoveAttributes ());

				prelink_subs.Add (new MarkNSObjects ());

				// subs.Add (new CoreHttpMessageHandler ());

				prelink_subs.Add (new PreserveSmartEnumConversionsSubStep ());

				// we can't subclass MarkStep and we need some information before it's executed
				InsertAfter (new ExtraMarkStep (), "MarkStep");

				// we can't subclass SweepStep and we need some information before it's executed
				InsertAfter (new PreSweepStep (), "ExtraMarkStep");

				// extra stuff filed as https://github.com/mono/linker/issues/1188
				InsertAfter (new ExtraSweepStep (), "SweepStep");

				var postlink_subs = new MobileSubStepDispatcher ();
				InsertAfter (postlink_subs, "CleanStep");

				// if (options.Application.Optimizations.ForceRejectedTypesRemoval == true)
				// 	sub.Add (new RemoveRejectedTypesStep ());

				postlink_subs.Add (new MetadataReducerSubStep ());

				// FIXME: need to determine if/when this is enabled https://github.com/mono/linker/blob/ffec224a2a69f0cde4c43d9c90090dcb294ca6c6/src/linker/Linker.Steps/SealerStep.cs#L24
				// 	if (options.Application.Optimizations.SealAndDevirtualize == true)
				// 		sub.Add (new SealerSubStep ());

				break;
			case LinkMode.All:
				break;
			case LinkMode.Platform:
				throw new NotSupportedException ();
			}

			// InsertAfter (new ListExportedSymbols (options.MarshalNativeExceptionsState), "OutputStep");

			if (Configuration.InsertTimestamps) {
				// note: some steps (e.g. BlacklistStep) dynamically adds steps to the pipeline
				for (int i = Steps.Count - 1; i >= 0; i--) {
					Steps.Insert (i + 1, new TimeStampStep (Steps [i].ToString ()));
				}
				Steps.Insert (0, new TimeStampStep ("Start"));
				TimeStampStep.Start ();
			}

			if (Configuration.InsaneVerbosity) {
				Configuration.Write ();
				Console.WriteLine ("Pipeline Steps:");
				foreach (var step in Steps) {
					Console.WriteLine ($"    {step}");
				}
			}
		}
	}

	public class DoNotLinkStep : ConfigurationAwareStep {

		Dictionary<string,AssemblyDefinition> defs = new Dictionary<string,AssemblyDefinition> ();
		HashSet<string> refs = new HashSet<string> ();

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			defs.Add (assembly.Name.Name, assembly);
			foreach (var m in assembly.Modules) {
				if (m.HasAssemblyReferences) {
					foreach (var reference in m.AssemblyReferences) {
						refs.Add (reference.Name);
					}
				}
			}
		}

		protected override void EndProcess ()
		{
			// promotion time! anything that is referenced must be Copy (not CopyUsed)
			foreach (var r in refs) {
				if (defs.TryGetValue (r, out var a)) {
					if (Annotations.GetAction (a) == AssemblyAction.CopyUsed)
						Annotations.SetAction (a, AssemblyAction.Copy);
				} else {
					Console.WriteLine ($"Could not find reference {r}");
				}
			}

			if (Configuration.InsaneVerbosity) {
				// in theory both numbers should be identical since we're not linking
				// but we can see references (from Xamarin.iOS.dll) to older versions of some assemblies
				Console.WriteLine ($"Assemblies ({defs.Count}) and references ({refs.Count} unique):");
				foreach (var a in defs.Values) {
					Console.WriteLine ($"    {a.Name}");
					foreach (var r in a.MainModule.AssemblyReferences)
						Console.WriteLine ($"        {r}");
				}
			}
		}
	}

	public class LinkSdkStep : ConfigurationAwareStep {

		// TODO move into some 'app data' object
		public static HashSet<AssemblyDefinition> defs = new HashSet<AssemblyDefinition> ();
		public static AssemblyDefinition PlatformAssembly;

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			// lack of `Context.GetAssemblies` API
			defs.Add (assembly);

			var name = assembly.Name.Name;
			switch (name) {
			case "Xamarin.Forms.Platform.iOS": // special case
				Annotations.SetAction (assembly, AssemblyAction.Link);
				break;
			case string _ when name == Configuration.PlatformAssembly:
				PlatformAssembly = assembly;
				break;
			}
		}
	}

	public class TimeStampStep : IStep {

		static Stopwatch watch = new Stopwatch ();
		readonly string message;

		public TimeStampStep (string message)
		{
			this.message = message;
		}

		public void Process (LinkContext context)
		{
			Console.WriteLine ($"Timestamp after {message}: {watch.ElapsedMilliseconds} ms");
		}

		public static void Start ()
		{
			watch.Start ();
		}
	}
}
