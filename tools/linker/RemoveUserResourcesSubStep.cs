using System;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;
using Mono.Tuner;

namespace Xamarin.Linker {

	public class RemoveUserResourcesSubStep : ExceptionalSubStep {

#if MTOUCH
		const string Content = "__monotouch_content_";
		const string Page = "__monotouch_page_";
#else
		const string Content = "__xammac_content_";
		const string Page = "__xammac_page_";
#endif
		public override SubStepTargets Targets {
			get { return SubStepTargets.Assembly; }
		}

#if !NET
		public bool Device { get { return LinkContext.App.IsDeviceBuild; } }
#endif

		protected override string Name { get; } = "Removing User Resources";
		protected override int ErrorCode { get; } = 2030;

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			// we know we do not ship assemblies with such resources so we can skip this step for our code
#if NET
			return !Profile.IsProductAssembly (assembly);
#else
			return !(Profile.IsProductAssembly (assembly) || Profile.IsSdkAssembly (assembly));
#endif
		}

		protected override void Process (AssemblyDefinition assembly)
		{
			var module = assembly.MainModule;
			if (!module.HasResources)
				return;
			
			HashSet<string> libraries = null;
			if (assembly.HasCustomAttributes) {
				foreach (var ca in assembly.CustomAttributes) {
					var libName = GetLinkWithLibraryName (ca);
					if (libName != null) {
						if (libraries == null)
							libraries = new HashSet<string> (StringComparer.OrdinalIgnoreCase);
						libraries.Add (libName);
					}
				}
			}

			var found = false;
			var resources = module.Resources;
			for (int i = 0; i < resources.Count; i++) {
				var resource = resources [i];

				if (!(resource is EmbeddedResource))
					continue;

				var name = resource.Name;
				if (!IsMonoTouchResource (name) && !IsNativeLibrary (name, libraries))
					continue;

				resources.RemoveAt (i--);
				found = true;
			}

			// we'll need to save (if we're not linking) this assembly
			if (found && Annotations.GetAction (assembly) != AssemblyAction.Link)
				Annotations.SetAction (assembly, AssemblyAction.Save);
		}

		// simplified version of Xamarin.Bundler.Assembly.GetLinkWithAttribute
		// with less allocations
		static string GetLinkWithLibraryName (CustomAttribute ca)
		{
			if (!ca.AttributeType.Is ("ObjCRuntime", "LinkWithAttribute"))
				return null;
			if (ca.HasConstructorArguments && ca.ConstructorArguments.Count > 1)
				return (string) ca.ConstructorArguments [0].Value; // first argument
			return null;
		}

		bool IsMonoTouchResource (string resourceName)
		{
#if MTOUCH && !NET
			if (!Device)
				return false;
#endif
			if (resourceName.StartsWith (Content, StringComparison.OrdinalIgnoreCase))
				return true;

			if (resourceName.StartsWith (Page, StringComparison.OrdinalIgnoreCase))
				return true;

			return false;
		}

		static bool IsNativeLibrary (string resourceName, HashSet<string> libraries)
		{
			if (libraries == null)
				return false;

			return libraries.Contains (resourceName);
		}
	}
}
