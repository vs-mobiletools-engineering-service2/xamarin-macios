using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Steps;
using Mono.Tuner;

namespace Xamarin.Linker.Steps {

	// TEMPORARY: macios subclassed SweepStep do sweep more code
	// https://github.com/mono/linker/issues/1188
	public class ExtraSweepStep : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			var action = Annotations.GetAction (assembly);
#if true
			// added in https://github.com/mono/linker/pull/1193
			if (action == AssemblyAction.Link) {
				// from MobileSweepStep
				// only when linking should we remove module references, if we (re)save the assembly then
				// the entrypoints (for p/invokes) will be required later
				// reference: https://bugzilla.xamarin.com/show_bug.cgi?id=35372
				if (assembly.MainModule.HasModuleReferences)
					SweepCollectionMetadata (assembly.MainModule.ModuleReferences);
			}
#endif
			if (action == AssemblyAction.Save || action == AssemblyAction.Link) {
#if true
				// from MobileSweepStep - already part of illink
				// if we save (only or by linking) then unmarked exports (e.g. forwarders) must be cleaned
				// or they can point to nothing which will break later (e.g. when re-loading for stripping IL)
				// reference: https://bugzilla.xamarin.com/show_bug.cgi?id=36577
				if (assembly.MainModule.HasExportedTypes)
					SweepCollectionMetadata (assembly.MainModule.ExportedTypes);
#endif
				// from MonoTouchMobileStep
				if (assembly.HasCustomAttributes)
					SweepInternalsVisibleToAttribute (assembly.CustomAttributes);
			}
		}

		void SweepInternalsVisibleToAttribute (IList<CustomAttribute> attributes)
		{
			for (int i = 0; i < attributes.Count; i++) {
				var ca = attributes [i];

				// we do not have to keep IVT to assemblies that are not part of the application
				if (!ca.AttributeType.Is ("System.Runtime.CompilerServices", "InternalsVisibleToAttribute"))
					continue;

				// validating the public key and the public key token would be time consuming
				// worse case (no match) is that we keep the attribute while it's not needed
				var fqn = (ca.ConstructorArguments [0].Value as string);
				int comma = fqn.IndexOf (',');
				if (comma != -1)
					fqn = fqn.Substring (0, comma);

				bool need_ivt = false;
				foreach (var assembly in LinkSdkStep.defs) {
					if (assembly.Name.Name == fqn) {
						if (Annotations.GetAction (assembly) != AssemblyAction.Delete) {
							need_ivt = true;
							break;
						}
					}
				}
				if (!need_ivt)
					attributes.RemoveAt (i--);
			}
		}

		// from SweepStep
		protected bool SweepCollectionMetadata<T> (IList<T> list) where T : IMetadataTokenProvider
		{
			bool removed = false;

			for (int i = 0; i < list.Count; i++) {
				if (ShouldRemove (list [i])) {
					list.RemoveAt (i--);
					removed = true;
				}
			}

			return removed;
		}

		protected virtual bool ShouldRemove<T> (T element) where T : IMetadataTokenProvider
		{
			return !Annotations.IsMarked (element);
		}
	}
}
