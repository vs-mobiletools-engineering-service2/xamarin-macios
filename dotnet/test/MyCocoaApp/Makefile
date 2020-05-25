TOP=../../..
include $(TOP)/Make.config

TEST_CONFIG?=Debug

all: publish

build: nugets
	dotnet build /v:diag /bl:build.binlog /p:Configuration=$(TEST_CONFIG)

publish: nugets
	dotnet publish --self-contained true /v:diag /bl:publish.binlog /p:Configuration=$(TEST_CONFIG)

clean:
	rm -Rf ~/.nuget/packages/microsoft.ios.* ~/.nuget/packages/microsoft.tvos.* ~/.nuget/packages/microsoft.watchos.* ~/.nuget/packages/microsoft.macos.*
	cd $(TOP)/msbuild && git clean -xfdq
	make clean -C $(TOP)/dotnet

nugets: clean
	make all install -C $(TOP)/tools/dotnet-linker
	make all install -j -C $(TOP)/msbuild
	make all install -j -C $(TOP)/dotnet

run: build
	dotnet run
