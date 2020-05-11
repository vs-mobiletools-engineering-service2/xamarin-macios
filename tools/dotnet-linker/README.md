
# Debug

## Debug with vscode

`illink` supports response files so you can do a simpler `launch.json` by using it, e.g.

```json
{
	// Use IntelliSense to learn about possible attributes.
	// Hover to view descriptions of existing attributes.
	// For more information, visit: https://go.microsoft.com/fwlink/?linkid=830387
	"version": "0.2.0",
	"configurations": [
		{
			"name": "illink - dontlink",
			"type": "coreclr",
			"request": "launch",
			"preLaunchTask": "build",
			"program": "/usr/local/share/dotnet/sdk/5.0.100-preview.4.20214.36/Sdks/ILLink.Tasks/tools/netcoreapp3.0/illink.dll",
			"justMyCode": false,
			"env": { "CUSTOM_LINKER_OPTIONS_FILE": "/Users/poupou/git/net5/xamarin-macios/msbuild/dotnet/test/MySingleView/obj/Debug/netcoreapp5.0/ios-x64/custom-linker-options.txt", },
			"args": [ "@/Users/poupou/git/net5/xamarin-macios/msbuild/dotnet/test/MySingleView/illink-dontlink.rsp" ],
			"cwd": "/Users/poupou/git/net5/xamarin-macios/msbuild/dotnet/test/MySingleView",
			"console": "internalConsole",
			"stopAtEntry": false
		},
	]
}
```

**NOTE: Fix all your paths in the .json and .rsp files !**

The content of the `.rsp` (response) file should be what `msbuild` gives to the `ILLink` task. You can get it from build logs (console or binary).

Right now an environment variable `CUSTOM_LINKER_OPTIONS_FILE` is used to provide extra configuration to the XI/XM linker steps. This is created by the msbuild files and must be part of the `launch.json` file.

You can add several configurations (e.g. linksdk, a different project...) to the same `launch.json` file.

You might also need to define the `build` task (reference from `launch.json`). A simple one would look like:

```json
{
	"version": "2.0.0",
	"tasks": [
		{
			"label": "build",
			"command": "dotnet",
			"type": "process",
			"args": [
				"build",
			],
			"problemMatcher": "$msCompile"
		}
	]
}
```

Without it you'll need to build manually before debugging in vscode.

# Useful Tools

## MSBuild Binary and Structured Log Viewer

[Instruction for macOS](https://github.com/KirillOsenkov/MSBuildStructuredLog#running-the-avalonia-version-on-mac)
