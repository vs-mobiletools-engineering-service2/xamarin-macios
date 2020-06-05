using System;

using Mono.Cecil;
using Mono.Linker;

using Xamarin.Bundler;
using Xamarin.Linker;

namespace Xamarin {

	// this is a partial version of the old Customize[iOS]Actions
	// that supports `[assembly:Preserve]` and `[assembly:LinkerSafe]`
	// the other parts are already done by other steps
	public class CustomizeActions : ConfigurationAwareStep {

		protected override bool ConditionToProcess ()
		{
			return Configuration.LinkMode != LinkMode.None;
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			// if we're linking the assembly...
			if (Annotations.GetAction (assembly) == AssemblyAction.Link) {
				// then we need to check for `[assembly:Preserve]` to override
				if (IsPreserved (assembly))
					Annotations.SetAction (assembly, AssemblyAction.CopyUsed);
			} else {
				// if we're not linking (e.g. Link SDK on user assembly) then we
				// then we need to check for `[assembly:LinkerSafe]` to override
				if (IsLinkerSafe (assembly))
					Annotations.SetAction (assembly, AssemblyAction.Link);
			}
		}

		// by design we lookup such attributes by name only as they can be part of user code
		static bool HasAttribute (AssemblyDefinition assembly, string name)
		{
			if (assembly.HasCustomAttributes) {
				foreach (var ca in assembly.CustomAttributes) {
					if (ca.HasConstructorArguments)
						return false;
					if (ca.AttributeType.Name == name)
						return true;
				}
			}
			return false;
		}

		static bool IsPreserved (AssemblyDefinition assembly)
		{
			// [assembly: Preserve (type)] does not preserve all the code in the assembly, in fact it might
			// not preserve anything in _this_ assembly, but something in a separate assembly (reference)
			return HasAttribute (assembly, "PreserveAttribute");
		}

		static bool IsLinkerSafe (AssemblyDefinition assembly)
		{
			return HasAttribute (assembly, "LinkerSafeAttribute");
		}
	}
}
