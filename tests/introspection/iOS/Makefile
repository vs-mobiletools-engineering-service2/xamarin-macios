TOP=../../..
include $(TOP)/Make.config

TEST_CONFIG?=Debug

all: build

build: build-ios build-tvos

build-ios: nugets
	dotnet build introspection-ios.dotnet.csproj       /bl:introspection.binlog /p:Configuration=$(TEST_CONFIG)

build-tvos: nugets
	dotnet build introspection-tvos.dotnet-tvos.csproj /bl:introspection.binlog /p:Configuration=$(TEST_CONFIG)

clean:
	rm -Rf ~/.nuget/packages/microsoft.ios.* ~/.nuget/packages/microsoft.tvos.* ~/.nuget/packages/microsoft.watchos.* ~/.nuget/packages/microsoft.macos.*
	rm -Rf $(TOP)/_build
	rm -Rf $(TOP)/dotnet/nupkgs
	cd $(TOP)/external/Touch.Unit/Touch.Client && git clean -xfd

nugets: clean
	make all install -j8 -C $(TOP)
	cp $(TOP)/dotnet/test/global.json $(TOP)/dotnet/test/NuGet.config .
	cp global.json NuGet.config ../../../external/Touch.Unit/Touch.Client

run: run-ios run-tvos
run-ios: run-ios-sim
run-tvos: run-tvos-sim

run-%-sim:
	$(MAKE) build-$*
	$(MAKE) run-$*-only

run-ios-only:
	$(IOS_DESTDIR)$(MONOTOUCH_PREFIX)/bin/mlaunch --launchsim $(abspath $(CURDIR)/bin/$(TEST_CONFIG)/net5.0/ios-x64/introspection.app)  --device=:v2:runtime=com.apple.CoreSimulator.SimRuntime.iOS-12-0,devicetype=com.apple.CoreSimulator.SimDeviceType.iPhone-6 --wait-for-exit --stderr="$(shell tty)" --stdout="$(shell tty)" "--setenv=MONO_TRACE=-E:all"

run-tvos-only:
	$(IOS_DESTDIR)$(MONOTOUCH_PREFIX)/bin/mlaunch --launchsim $(abspath $(CURDIR)/bin/$(TEST_CONFIG)/net5.0/tvos-x64/introspection.app) --device=:v2:runtime=com.apple.CoreSimulator.SimRuntime.tvOS-13-4,devicetype=com.apple.CoreSimulator.SimDeviceType.Apple-TV-1080p --wait-for-exit --stderr="$(shell tty)" --stdout="$(shell tty)" "--setenv=MONO_TRACE=-E:all"
