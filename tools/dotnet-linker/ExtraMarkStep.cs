using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Xamarin.Linker.Steps {

	// TEMPORARY: macios subclassed MarkStep do mark more code
	// https://github.com/mono/linker/issues/1188
	public class ExtraMarkStep : BaseStep {

		// adapted from MobileMarkStep
		// added in https://github.com/mono/linker/pull/1193
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			// deal with [TypeForwardedTo] pseudo-attributes
#if true // already in illink
			if (assembly.MainModule.HasExportedTypes) {
				foreach (var exported in assembly.MainModule.ExportedTypes) {
					if (!exported.IsForwarder)
						continue;
					var type = exported.Resolve ();
					if (!Annotations.IsMarked (type))
						continue;
#if DEBUG
					if (!Annotations.IsMarked (exported))
#endif
					Annotations.Mark (exported);
				}
			}
#endif

#if true
			// added in https://github.com/mono/linker/pull/1193
			foreach (var module in assembly.Modules) {
				if (!module.HasTypes)
					continue;
				ProcessTypes (module.Types);
			}
#endif
		}

		void ProcessTypes (IList<TypeDefinition> types)
		{
			foreach (var type in types) {
				if (type.HasNestedTypes)
					ProcessTypes (type.NestedTypes);
				if (!type.HasMethods)
					continue;
				foreach (var method in type.Methods) {
					if (!method.IsPInvokeImpl)
						continue;
					if (!Annotations.IsMarked (method))
						continue;
					// for some C++ stuff HasPInvokeInfo can be true without giving back any info
					PInvokeInfo info = method.PInvokeInfo;
					if (info != null) {
						var m = info.Module;
#if DEBUG
						if (!Annotations.IsMarked (m))
#endif
						Annotations.Mark (m);
					}
				}
			}
		}
	}
}
