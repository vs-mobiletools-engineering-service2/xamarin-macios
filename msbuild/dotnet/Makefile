TOP = ../..

include $(TOP)/Make.config
include $(TOP)/mk/rules.mk

ifeq ($(V),)
DOTNET_PACK_VERBOSITY=--verbosity:quiet --nologo
NUGET_VERBOSITY=-verbosity quiet
else
DOTNET_PACK_VERBOSITY=--verbosity:detailed
NUGET_VERBOSITY=-verbosity detailed
endif

TEST_FEED_PATH=test/feed

PLATFORMS=iOS tvOS watchOS macOS

IOS_RIDS=arm64 arm x86 x64
TVOS_RIDS=arm64 x64
WATCHOS_RIDS=arm x86
MACOS_RIDS=x64

DIRECTORIES += \

TARGETS += $(IOS_TARGETS) $(TVOS_TARGETS) $(WATCHOS_TARGETS) $(MACOS_TARGETS)

install-local::

$(DIRECTORIES):
	$(Q) mkdir -p $@

CURRENT_HASH_LONG:=$(shell git log -1 --pretty=%H)

targets/%.props: targets/%.template.props Makefile $(TOP)/Make.config.inc
	$(Q_GEN) sed \
		-e "s/@IOS_NUGET_VERSION_NO_METADATA@/$(IOS_NUGET_VERSION_NO_METADATA)/g" \
		-e "s/@TVOS_NUGET_VERSION_NO_METADATA@/$(TVOS_NUGET_VERSION_NO_METADATA)/g" \
		-e "s/@WATCHOS_NUGET_VERSION_NO_METADATA@/$(WATCHOS_NUGET_VERSION_NO_METADATA)/g" \
		-e "s/@MACOS_NUGET_VERSION_NO_METADATA@/$(MACOS_NUGET_VERSION_NO_METADATA)/g" \
		-e "s/@IOS_NUGET_VERSION_FULL@/$(IOS_NUGET_VERSION_FULL)/g" \
		-e "s/@TVOS_NUGET_VERSION_FULL@/$(TVOS_NUGET_VERSION_FULL)/g" \
		-e "s/@WATCHOS_NUGET_VERSION_FULL@/$(WATCHOS_NUGET_VERSION_FULL)/g" \
		-e "s/@MACOS_NUGET_VERSION_FULL@/$(MACOS_NUGET_VERSION_FULL)/g" \
		-e "s/@CURRENT_BRANCH@/$(CURRENT_BRANCH_SED_ESCAPED)/g" \
		-e "s/@CURRENT_HASH_LONG@/$(CURRENT_HASH_LONG)/g" \
	$< > $@

TEMPLATED_FILES = \
	targets/Xamarin.Shared.Sdk.Versions.props \

all-local:: $(TARGETS) targets/Xamarin.Shared.Sdk.Versions.props

pack: ref-pack sdk-pack runtime-pack

# just a temporary target while debugging for faster turnaround
packsimple: prepare
	$(Q) rm -Rf nupkgs/*
	$(Q) $(MAKE) ref-pack-iOS sdk-pack-iOS nupkgs/Microsoft.iOS.Runtime.ios-x64.nupkg nupkgs/Microsoft.iOS.Runtime.ios-arm64.nupkg

nupkgs/Microsoft.iOS.%.nupkg: CURRENT_VERSION=$(IOS_NUGET_VERSION_NO_METADATA)
nupkgs/Microsoft.tvOS.%.nupkg: CURRENT_VERSION=$(TVOS_NUGET_VERSION_NO_METADATA)
nupkgs/Microsoft.watchOS.%.nupkg: CURRENT_VERSION=$(WATCHOS_NUGET_VERSION_NO_METADATA)
nupkgs/Microsoft.macOS.%.nupkg: CURRENT_VERSION=$(MACOS_NUGET_VERSION_NO_METADATA)

nupkgs/%.nupkg: prepare $(TEMPLATED_FILES)
	$(Q) rm -f $@ nupkgs/$*.*.nupkg
	$(Q) rm -Rf $(TEST_FEED_PATH)/$(shell echo $* | tr '[:upper:]' '[:lower:]')
	$(Q) rm -Rf $(HOME)/.nuget/packages/$(shell echo $* | tr '[:upper:]' '[:lower:]')
	$(if $(V),,@echo "PACK     $@";) $(DOTNET) pack package/$*/package.csproj --output nupkgs $(DOTNET_PACK_VERBOSITY) /bl:package/$*/obj/$*.binlog
	@# Rename nuget to contain exact version
	$(Q) mv "nupkgs/$*.$(CURRENT_VERSION).nupkg" "nupkgs/$*.$(CURRENT_VERSION)+sha.$(CURRENT_HASH).nupkg"
	$(Q) echo "Created: nupkgs/$*+sha.$(CURRENT_HASH).nupkg"

install-nugets:
	$(Q) rm -Rf $(TEST_FEED_PATH)
	$(Q) mkdir -p $(TEST_FEED_PATH)
	$(Q) for nupkg in nupkgs/*.nupkg; do \
		echo "ADD `basename $$nupkg`"; \
		nuget add "$$nupkg" -source $(TEST_FEED_PATH) $(NUGET_VERBOSITY); \
	done

runtime-pack-iOS: $(foreach rid,$(IOS_RIDS),nupkgs/Microsoft.iOS.Runtime.ios-$(rid).nupkg)
runtime-pack-tvOS: $(foreach rid,$(TVOS_RIDS),nupkgs/Microsoft.tvOS.Runtime.tvos-$(rid).nupkg)
runtime-pack-watchOS: $(foreach rid,$(WATCHOS_RIDS),nupkgs/Microsoft.watchOS.Runtime.watchos-$(rid).nupkg)
runtime-pack-macOS: $(foreach rid,$(MACOS_RIDS),nupkgs/Microsoft.macOS.Runtime.osx-$(rid).nupkg)

ref-pack-%: nupkgs/Microsoft.%.Ref.nupkg;
ref-pack: $(foreach var,$(PLATFORMS),nupkgs/Microsoft.$(var).Ref.nupkg)
sdk-pack-%: nupkgs/Microsoft.%.Sdk.nupkg;
sdk-pack: $(foreach var,$(PLATFORMS),nupkgs/Microsoft.$(var).Sdk.nupkg)
runtime-pack: $(foreach var,$(PLATFORMS),runtime-pack-$(var))

pack-%:
	$(MAKE) ref-pack-$* runtime-pack-$* sdk-pack-$*

test/NuGet.config: NuGet.config Makefile
	$(Q) $(CP) $< $@.tmp
	$(Q) nuget sources add -Name local-dotnet-feed -Source $(abspath $(CURDIR)/$(TEST_FEED_PATH)) -ConfigFile $@.tmp
	$(Q) mv $@.tmp $@

test/global.json: Makefile $(TEMPLATED_FILES)
	$(Q_GEN) \
		printf "{\n" > $@; \
		printf "\t\"sdk\": { \"version\": \"5.*\" },\n" >> $@; \
		printf "\t\"msbuild-sdks\": {\n" >> $@; \
		printf "\t\t\"Microsoft.iOS.Sdk\": \"$(IOS_NUGET_VERSION_NO_METADATA)\",\n" >> $@; \
		printf "\t\t\"Microsoft.tvOS.Sdk\": \"$(TVOS_NUGET_VERSION_NO_METADATA)\",\n" >> $@; \
		printf "\t\t\"Microsoft.watchOS.Sdk\": \"$(WATCHOS_NUGET_VERSION_NO_METADATA)\",\n" >> $@; \
		printf "\t\t\"Microsoft.macOS.Sdk\": \"$(MACOS_NUGET_VERSION_NO_METADATA)\"\n" >> $@; \
		printf "\t}\n}\n" >> $@

test-nuget: test/NuGet.config test/global.json
	$(Q) $(MAKE) packsimple
	@# Clear out anything from the nuget cache from previous tests
	$(Q) rm -Rf $(HOME)/.nuget/packages/xamarin.*.sdk
	@#$(if $(V),,@echo "BUILD    MySingleView.app";) $(DOTNET) build test/MySingleView/MySingleView.csproj /p:Platform=iPhone $(XBUILD_VERBOSITY)
	$(if $(V),,@echo "BUILD    MySingleView.app";) $(DOTNET) build test/MySingleView/MySingleView.csproj /p:Platform=iPhoneSimulator $(XBUILD_VERBOSITY)
	@#$(if $(V),,@echo "BUILD    MyCocoaApp.app";)   $(DOTNET) build test/MyCocoaApp/MyCocoaApp.csproj $(XBUILD_VERBOSITY)

prepare:
	$(Q) V=$(V) \
	TOP=$(TOP) \
	DOTNET_DESTDIR=$(DOTNET_DESTDIR) \
	MAC_DESTDIR=$(MAC_DESTDIR) \
	IOS_DESTDIR=$(IOS_DESTDIR) \
	MAC_FRAMEWORK_DIR=$(MAC_FRAMEWORK_DIR) \
	MONOTOUCH_PREFIX=$(MONOTOUCH_PREFIX) \
	./build-nugets.sh
