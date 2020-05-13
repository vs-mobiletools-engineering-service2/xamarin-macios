using System;
using System.IO;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

using Xamarin.Utils;

namespace Xamarin.MacDev.Tasks {
	// This is the same as XamarinTask, except that it subclasses ToolTask instead.
	public abstract class XamarinToolTask : ToolTask {

		public string SessionId { get; set; }

		public string TargetFrameworkMoniker { get; set; }

		void VerifyTargetFrameworkMoniker ()
		{
			if (!string.IsNullOrEmpty (TargetFrameworkMoniker))
				return;
			Log.LogError ($"The task {GetType ().Name} requires TargetFrameworkMoniker to be set.");
		}

		public string Product {
			get {
				switch (Platform) {
				case ApplePlatform.iOS:
				case ApplePlatform.TVOS:
				case ApplePlatform.WatchOS:
					return "Xamarin.iOS";
				case ApplePlatform.MacOSX:
					return "Xamarin.Mac";
				default:
					throw new InvalidOperationException ($"Invalid platform: {Platform}");
				}
			}
		}

		ApplePlatform? platform;
		public ApplePlatform Platform {
			get {
				if (!platform.HasValue)
					platform = PlatformFrameworkHelper.GetFramework (TargetFrameworkMoniker);
				return platform.Value;
			}
		}

		TargetFramework? target_framework;
		public TargetFramework TargetFramework {
			get {
				if (!target_framework.HasValue) {
					VerifyTargetFrameworkMoniker ();
					target_framework = TargetFramework.Parse (TargetFrameworkMoniker);
				}
				return target_framework.Value;
			}
		}

		public string PlatformName {
			get {
				switch (Platform) {
				case ApplePlatform.iOS:
					return "iOS";
				case ApplePlatform.TVOS:
					return "tvOS";
				case ApplePlatform.WatchOS:
					return "watchOS";
				case ApplePlatform.MacOSX:
					return "macOS";
				default:
					throw new InvalidOperationException ($"Invalid platform: {Platform}");
				}
			}
		}

		public string GetAppManifest (string appBundlePath)
		{
			switch (Platform) {
			case ApplePlatform.iOS:
			case ApplePlatform.WatchOS:
			case ApplePlatform.TVOS:
				return Path.Combine (appBundlePath, "Info.plist");
			case ApplePlatform.MacOSX:
				return Path.Combine (appBundlePath, "Contents", "Info.plist");
			default:
				throw new InvalidOperationException ($"Invalid platform: {Platform}");
			}
		}

		public string GetDeploymentTarget (string appBundlePath)
		{
			var manifest = GetAppManifest (appBundlePath);
			var plist = PDictionary.FromFile (manifest);
			switch (Platform) {
			case ApplePlatform.iOS:
			case ApplePlatform.WatchOS:
			case ApplePlatform.TVOS:
				return plist.GetMinimumOSVersion ();
			case ApplePlatform.MacOSX:
				return plist.GetMinimumSystemVersion ();
			default:
				throw new InvalidOperationException ($"Invalid platform: {Platform}");
			}
		}
	}
}
