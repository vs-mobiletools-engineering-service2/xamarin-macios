TOP=../../..
include $(TOP)/Make.config

TEST_CONFIG?=Debug

all: publish

build: nugets
	msbuild /r $(TOP)/external/Touch.Unit/Touch.Client/Touch.Client.csproj
	dotnet build *dotnet* /bl:introspection.binlog /p:Configuration=$(TEST_CONFIG)

clean:
	rm -Rf ~/.nuget/packages/microsoft.ios.* ~/.nuget/packages/microsoft.tvos.* ~/.nuget/packages/microsoft.watchos.* ~/.nuget/packages/microsoft.macos.*
	cd $(TOP)/external/Touch.Unit/Touch.Client && git clean -xfd

nugets: clean
	make all install -C $(TOP)/tools/dotnet-linker
	make all install -j -C $(TOP)/msbuild
	make all install -j -C $(TOP)/dotnet
	cp $(TOP)/dotnet/test/global.json $(TOP)/dotnet/test/NuGet.config .

run: run-sim

run-sim:
	$(MAKE) build
	$(MAKE) run-only

run-only:
	$(IOS_DESTDIR)$(MONOTOUCH_PREFIX)/bin/mlaunch --launchsim $(abspath $(CURDIR)/bin/$(TEST_CONFIG)/netcoreapp5.0/ios-x64/introspection.app) --device=:v2:runtime=com.apple.CoreSimulator.SimRuntime.iOS-12-0,devicetype=com.apple.CoreSimulator.SimDeviceType.iPhone-6 --wait-for-exit --stderr="$(shell tty)" --stdout="$(shell tty)" "--setenv=MONO_TRACE=-E:all"
