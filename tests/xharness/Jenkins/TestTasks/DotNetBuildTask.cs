using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Microsoft.DotNet.XHarness.iOS.Shared;
using Microsoft.DotNet.XHarness.iOS.Shared.Execution;
using Microsoft.DotNet.XHarness.iOS.Shared.Logging;

using Xharness.TestTasks;

namespace Xharness.Jenkins.TestTasks {
	class DotNetBuildTask : MSBuildTask {

		public DotNetBuildTask (Jenkins jenkins, TestProject testProject, IProcessManager processManager) 
			: base (jenkins, testProject, processManager) { }

		protected override string ToolName => Jenkins.Harness.GetDotNetExecutable (Path.GetDirectoryName (ProjectFile));

		public override bool RestoreNugets => false; // 'dotnet build' will restore

		public override void SetEnvironmentVariables (Process process)
		{
			base.SetEnvironmentVariables (process);
			// modify those env vars that we do care about

			process.StartInfo.EnvironmentVariables.Remove ("MSBUILD_EXE_PATH");
			process.StartInfo.EnvironmentVariables.Remove ("MSBuildExtensionsPathFallbackPathsOverride");
			process.StartInfo.EnvironmentVariables.Remove ("MSBuildSDKsPath");
			process.StartInfo.EnvironmentVariables.Remove ("TargetFrameworkFallbackSearchPaths");
			process.StartInfo.EnvironmentVariables.Remove ("MSBuildExtensionsPathFallbackPathsOverride");
		}

		protected override void InitializeTool () =>
			buildToolTask = new DotNetBuild (
				msbuildPath: () => ToolName,
				processManager: ProcessManager,
				resourceManager: ResourceManager,
				eventLogger: this,
				envManager: this,
				errorKnowledgeBase: Jenkins.ErrorKnowledgeBase);

		public static void SetDotNetEnvironmentVariables (Dictionary<string, string> environment)
		{
			environment ["MSBUILD_EXE_PATH"] = null;
			environment ["MSBuildExtensionsPathFallbackPathsOverride"] = null;
			environment ["MSBuildSDKsPath"] = null;
			environment ["TargetFrameworkFallbackSearchPaths"] = null;
			environment ["MSBuildExtensionsPathFallbackPathsOverride"] = null;
		}

		public static void CopyDotNetTestFiles (ILog log, string target_directory)
		{
			// The global.json and NuGet.config files make sure we use the locally built packages.
			var dotnet_test_dir = Path.Combine (Xharness.HarnessConfiguration.RootDirectory, "..", "tests", "dotnet");
			var global_json = Path.Combine (dotnet_test_dir, "global.json");
			var nuget_config = Path.Combine (dotnet_test_dir, "NuGet.config");
			File.Copy (global_json, Path.Combine (Path.GetDirectoryName (target_directory), Path.GetFileName (global_json)), true);
			log.WriteLine ($"Copied {global_json} to {target_directory}");
			File.Copy (nuget_config, Path.Combine (Path.GetDirectoryName (target_directory), Path.GetFileName (nuget_config)), true);
			log.WriteLine ($"Copied {nuget_config} to {target_directory}");
		}

		protected override void BeforeBuild ()
		{
			base.BeforeBuild ();

			CopyDotNetTestFiles (BuildLog, Path.GetDirectoryName (TestProject.Path));
		}
	}
}
