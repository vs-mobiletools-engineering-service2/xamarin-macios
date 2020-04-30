// Compat.cs: might not be ideal but ease code sharing with existing implementation

using System;
using Mono.Cecil;

namespace Xamarin.Tuner { }

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
}
