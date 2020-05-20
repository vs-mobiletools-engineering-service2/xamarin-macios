using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker.Steps;
using Xamarin.Tuner;

namespace Xamarin.Linker.Steps {

	// Historical note:
	// CoreSweepStep subclassed SweepStep in order to be notified if a
	// type was not marked (to be removed) and for unmarked interfaces.
	// This is because the (removed) information is required later by the
	// static registrar - but we still don't want it inside the final app
	public class PreSweepStep : BaseStep {

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			foreach (var module in assembly.Modules) {
				if (module.HasTypes)
					ProcessTypes (module.Types);
			}
		}

		DerivedLinkContext DerivedLinkContext => DerivedLinkContext.Instance;

		void ProcessTypes (IList<TypeDefinition> types)
		{
			foreach (var type in types) {
				if (type.HasNestedTypes)
					ProcessTypes (type.NestedTypes);

				if (Annotations.IsMarked (type)) {
					if (type.HasInterfaces) {
						ProcessInterfaces (type);
					}
				} else {
					// the type has not been marked (IOW it's to be removed)
					// so we keep a copy around for the static registrar who might need it later
					DerivedLinkContext.AddLinkedAwayType (type);
				}
			}
		}

		void ProcessInterfaces (TypeDefinition type)
		{
			for (int i = type.Interfaces.Count - 1; i >= 0; i--) {
				var iface = type.Interfaces [i];
				if (Annotations.IsMarked (iface))
					continue;

				if (!DerivedLinkContext.ProtocolImplementations.TryGetValue (type, out var list))
					DerivedLinkContext.ProtocolImplementations [type] = list = new List<TypeDefinition> ();
				var it = iface.InterfaceType.Resolve ();
				if (it == null) {
					// The interface type might already have been linked away, so go look for it among those types as well
					it = DerivedLinkContext.GetLinkedAwayType (iface.InterfaceType, out _);
				}
				list.Add (it);
			}
		}
	}
}
