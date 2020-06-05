using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Mono.Linker;

using Xamarin.Bundler;
using Xamarin.Utils;

namespace Xamarin.Linker {
	public class LinkerConfiguration {
		public ApplePlatform Platform { get; private set; }
		public string PlatformAssembly { get; private set; }
		public LinkMode LinkMode { get; private set; }

		public bool DebugBuild { get; private set; }

		public bool InsertTimestamps { get; private set; } = true;

		public bool InsaneVerbosity { get; private set; } = true;

		public List<string> WarnOnTypeRef { get; } = new List<string> ();

		LinkerConfiguration ()
		{
			// not the historical tooling (mtouch) default, but that's what the simulator templates offered
			LinkMode = LinkMode.None;
		}

		static ConditionalWeakTable<LinkContext, LinkerConfiguration> configurations = new ConditionalWeakTable<LinkContext, LinkerConfiguration> ();
		static LinkerConfiguration single_instance;
		public static LinkerConfiguration Instance {
			get {
				return single_instance;
			}
		}

		public static LinkerConfiguration GetInstance (LinkContext context)
		{
			if (!configurations.TryGetValue (context, out var instance)) {
				if (!context.TryGetCustomData ("LinkerOptionsFile", out var linker_options_file))
					throw new Exception ($"No custom linker options file was passed to the linker (using --custom-data LinkerOptionsFile=...");
				instance = new LinkerConfiguration (linker_options_file);
				single_instance = instance;
				configurations.Add (context, instance);
			}

			return instance;
		}

		LinkerConfiguration (string linker_file)
		{
			if (!File.Exists (linker_file))
				throw new FileNotFoundException ($"The custom linker file {linker_file} does not exist.");

			var lines = File.ReadAllLines (linker_file);
			for (var i = 0; i < lines.Length; i++) {
				var line = lines [i].TrimStart ();
				if (line.Length == 0 || line [0] == '#')
					continue; // Allow comments

				var eq = line.IndexOf ('=');
				if (eq == -1)
					throw new InvalidOperationException ($"Invalid syntax for line {i + 1} in {linker_file}: No equals sign.");

				var key = line [..eq];
				var value = line [(eq + 1)..];
				switch (key) {
				case "LinkMode":
					switch (value.ToLowerInvariant ()) {
					case "full":
						LinkMode = LinkMode.All;
						break;
					case "sdkonly":
						LinkMode = LinkMode.SDKOnly;
						break;
					case "platform":
						LinkMode = LinkMode.Platform;
						break;
					case "none":
						LinkMode = LinkMode.None;
						break;
					default:
						throw new InvalidOperationException ($"Unknown link mode: {value} for the entry {line} in {linker_file}");
					}
					break;
				case "Platform":
					switch (value) {
					case "iOS":
						Platform = ApplePlatform.iOS;
						break;
					case "tvOS":
						Platform = ApplePlatform.TVOS;
						break;
					case "watchOS":
						Platform = ApplePlatform.WatchOS;
						break;
					case "macOS":
						Platform = ApplePlatform.MacOSX;
						break;
					default:
						throw new InvalidOperationException ($"Unknown platform: {value} for the entry {line} in {linker_file}");
					}
					break;
				case "PlatformAssembly":
					PlatformAssembly = Path.GetFileNameWithoutExtension (value);
					break;
				default:
					throw new InvalidOperationException ($"Unknown key '{key}' in {linker_file}");
				}
			}
		}

		public void Write ()
		{
			Console.WriteLine ($"LinkerConfiguration:");
			Console.WriteLine ($"    LinkMode: {LinkMode}");
			Console.WriteLine ($"    Platform: {Platform}");
			Console.WriteLine ($"    PlatformAssembly: {PlatformAssembly}.dll");
		}
	}
}
