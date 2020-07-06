// Compat.cs: might not be ideal but it eases code sharing with existing code during the initial implementation.
using System;

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
	public class Application {
		public LinkerConfiguration Configuration { get; private set; }

		public Application (LinkerConfiguration configuration)
		{
			this.Configuration = configuration;
		}

		// This method is needed for ErrorHelper.tools.cs to compile.
		public void LoadSymbols ()
		{
		}

		public string GetProductName ()
		{
			switch (Platform) {
			case ApplePlatform.iOS:
			case ApplePlatform.TVOS:
			case ApplePlatform.WatchOS:
				return "Xamarin.iOS";
			case ApplePlatform.MacOSX:
				return "Xamarin.Mac";
			default:
				throw ErrorHelper.CreateError (177, Errors.MX0177 /* "Unknown platform: {0}. This usually indicates a bug; please file a bug report at https://github.com/xamarin/xamarin-macios/issues/new with a test case." */, Platform);
			}
		}

		public Version SdkVersion {
			get { return Configuration.SdkVersion; }
		}

		public Version DeploymentTarget {
			get { return Configuration.DeploymentTarget; }
		}

		public bool IsSimulatorBuild {
			get { return Configuration.IsSimulatorBuild; }
		}

		public ApplePlatform Platform {
			get { return Configuration.Platform; }
		}
	}
}
