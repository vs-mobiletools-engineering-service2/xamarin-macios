#if true

// Compat.cs: might not be ideal but ease code sharing with existing implementation

using System;
using Mono.Cecil;
using Mono.Tuner;
using Xamarin.Bundler;

namespace Xamarin.Linker {

	public static class ErrorHelper {
		public static Exception CreateError (int code, Exception innerException, string message)
		{
			// fix type
			return new NotFiniteNumberException (message, innerException);
		}

		public static Exception CreateError (int code, string message)
		{
			// fix type
			return new NotFiniteNumberException (message);
		}

		public static Exception CreateWarning (int code, ICustomAttributeProvider provider, string message, params object [] args)
		{
			return new NotFiniteNumberException (String.Format (message, args));
		}

		public static void Show (Exception exception)
		{
			Console.WriteLine (exception.ToString ());
		}
	}

	public static class Driver {
		public static void Log (int logLevel, string message)
		{
			Console.WriteLine (message);
		}

		public static void Log (int logLevel, string message, params object [] args)
		{
			Console.WriteLine (message, args);
		}
	}

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

#endif
