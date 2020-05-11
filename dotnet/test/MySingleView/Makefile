TOP=../../..
include $(TOP)/Make.config

TEST_CONFIG?=Debug

all: publish

build: nugets
	dotnet build /v:diag /bl:MySingleView-build.binlog /p:Configuration=$(TEST_CONFIG)

publish: nugets
	dotnet publish --self-contained true /v:diag /bl:MySingleView-publish.binlog /p:Configuration=$(TEST_CONFIG)

clean:
	rm -Rf ~/.nuget/packages/microsoft.ios.* ~/.nuget/packages/microsoft.tvos.* ~/.nuget/packages/microsoft.watchos.* ~/.nuget/packages/microsoft.macos.*
	cd $(TOP)/msbuild && git clean -xfdq
	make clean -C $(TOP)/dotnet

nugets: clean
	make all install -C $(TOP)/tools/dotnet-linker
	make all install -j -C $(TOP)/msbuild
	make all install -j -C $(TOP)/dotnet

run: run-sim

run-sim:
	$(MAKE) build
	$(MAKE) run-only

run-only:
	$(IOS_DESTDIR)$(MONOTOUCH_PREFIX)/bin/mlaunch --launchsim $(abspath $(CURDIR)/bin/$(TEST_CONFIG)/netcoreapp5.0/ios-x64/MySingleView.app) --device=:v2:runtime=com.apple.CoreSimulator.SimRuntime.iOS-12-0,devicetype=com.apple.CoreSimulator.SimDeviceType.iPhone-6 --wait-for-exit --stderr="$(shell tty)" --stdout="$(shell tty)" "--setenv=MONO_TRACE=-E:all"
