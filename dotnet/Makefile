TOP = ..

include $(TOP)/Make.config
include $(TOP)/mk/rules.mk

ifeq ($(V),)
DOTNET_PACK_VERBOSITY=--verbosity:quiet --nologo
NUGET_VERBOSITY=-verbosity quiet
else
DOTNET_PACK_VERBOSITY=--verbosity:detailed
NUGET_VERBOSITY=-verbosity detailed
endif
Q_PACK     =$(if $(V),,@echo "PACK      $@";)
Q_NUGET_ADD=$(if $(V),,@echo "NUGET ADD $@";)

PLATFORMS=iOS tvOS watchOS macOS

IOS_RIDS=arm64 arm x86 x64
TVOS_RIDS=arm64 x64
WATCHOS_RIDS=arm x86
MACOS_RIDS=x64

DIRECTORIES += \
	$(DOTNET_NUPKG_DIR) \
	$(DOTNET_FEED_DIR) \

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

nupkgs/Microsoft.iOS.%.nupkg: CURRENT_VERSION_NO_METADATA=$(IOS_NUGET_VERSION_NO_METADATA)
nupkgs/Microsoft.tvOS.%.nupkg: CURRENT_VERSION_NO_METADATA=$(TVOS_NUGET_VERSION_NO_METADATA)
nupkgs/Microsoft.watchOS.%.nupkg: CURRENT_VERSION_NO_METADATA=$(WATCHOS_NUGET_VERSION_NO_METADATA)
nupkgs/Microsoft.macOS.%.nupkg: CURRENT_VERSION_NO_METADATA=$(MACOS_NUGET_VERSION_NO_METADATA)

nupkgs/Microsoft.iOS.%.nupkg: CURRENT_VERSION_FULL=$(IOS_NUGET_VERSION_FULL)
nupkgs/Microsoft.tvOS.%.nupkg: CURRENT_VERSION_FULL=$(TVOS_NUGET_VERSION_FULL)
nupkgs/Microsoft.watchOS.%.nupkg: CURRENT_VERSION_FULL=$(WATCHOS_NUGET_VERSION_FULL)
nupkgs/Microsoft.macOS.%.nupkg: CURRENT_VERSION_FULL=$(MACOS_NUGET_VERSION_FULL)

# Create the nuget in a temporary directory (nupkgs/)
nupkgs/%.nupkg: prepare $(TEMPLATED_FILES)
	$(Q) rm -f $@
	$(Q_PACK) $(DOTNET) pack package/$(shell echo $(notdir $@) | sed 's/[.]$(CURRENT_VERSION_FULL).*//')/package.csproj --output "$(dir $@)" $(DOTNET_PACK_VERBOSITY)
	@# Nuget pack doesn't add the metadata to the filename, but we want that, so rename nuget to contain the full name
	$(Q) mv "nupkgs/$(shell echo $(notdir $@) | sed -e 's/[+]$(NUGET_BUILD_METADATA)//')" "$@"
	@# Add the nupkg to our local feed
	$(Q_NUGET_ADD) nuget add "$@" -source $(DOTNET_FEED_DIR) $(NUGET_VERBOSITY)

# Copy the nuget from the temporary directory into the final directory
$(DOTNET_NUPKG_DIR)/%.nupkg: nupkgs/%.nupkg | $(DOTNET_NUPKG_DIR)
	$(Q) cp $< $@

RUNTIME_PACKS_IOS = $(foreach rid,$(IOS_RIDS),$(DOTNET_NUPKG_DIR)/Microsoft.iOS.Runtime.ios-$(rid).$(IOS_NUGET_VERSION_FULL).nupkg)
RUNTIME_PACKS_TVOS = $(foreach rid,$(TVOS_RIDS),$(DOTNET_NUPKG_DIR)/Microsoft.tvOS.Runtime.tvos-$(rid).$(TVOS_NUGET_VERSION_FULL).nupkg)
RUNTIME_PACKS_WATCHOS = $(foreach rid,$(WATCHOS_RIDS),$(DOTNET_NUPKG_DIR)/Microsoft.watchOS.Runtime.watchos-$(rid).$(WATCHOS_NUGET_VERSION_FULL).nupkg)
RUNTIME_PACKS_MACOS = $(foreach rid,$(MACOS_RIDS),$(DOTNET_NUPKG_DIR)/Microsoft.macOS.Runtime.osx-$(rid).$(MACOS_NUGET_VERSION_FULL).nupkg)
RUNTIME_PACKS = $(RUNTIME_PACKS_IOS) $(RUNTIME_PACKS_TVOS) $(RUNTIME_PACKS_WATCHOS) $(RUNTIME_PACKS_MACOS)

REF_PACK_IOS = $(DOTNET_NUPKG_DIR)/Microsoft.iOS.Ref.$(IOS_NUGET_VERSION_FULL).nupkg
REF_PACK_TVOS = $(DOTNET_NUPKG_DIR)/Microsoft.tvOS.Ref.$(TVOS_NUGET_VERSION_FULL).nupkg
REF_PACK_WATCHOS = $(DOTNET_NUPKG_DIR)/Microsoft.watchOS.Ref.$(WATCHOS_NUGET_VERSION_FULL).nupkg
REF_PACK_MACOS = $(DOTNET_NUPKG_DIR)/Microsoft.macOS.Ref.$(MACOS_NUGET_VERSION_FULL).nupkg
REF_PACKS = $(REF_PACK_IOS) $(REF_PACK_TVOS) $(REF_PACK_WATCHOS) $(REF_PACK_MACOS)

SDK_PACK_IOS = $(DOTNET_NUPKG_DIR)/Microsoft.iOS.Sdk.$(IOS_NUGET_VERSION_FULL).nupkg
SDK_PACK_TVOS = $(DOTNET_NUPKG_DIR)/Microsoft.tvOS.Sdk.$(TVOS_NUGET_VERSION_FULL).nupkg
SDK_PACK_WATCHOS = $(DOTNET_NUPKG_DIR)/Microsoft.watchOS.Sdk.$(WATCHOS_NUGET_VERSION_FULL).nupkg
SDK_PACK_MACOS = $(DOTNET_NUPKG_DIR)/Microsoft.macOS.Sdk.$(MACOS_NUGET_VERSION_FULL).nupkg
SDK_PACKS = $(SDK_PACK_IOS) $(SDK_PACK_TVOS) $(SDK_PACK_WATCHOS) $(SDK_PACK_MACOS)

pack-ios: $(RUNTIME_PACKS_IOS) $(REF_PACK_IOS) $(SDK_PACK_IOS)
pack-tvos: $(RUNTIME_PACKS_TVOS) $(REF_PACK_TVOS) $(SDK_PACK_TVOS)
pack-watchos: $(RUNTIME_PACKS_WATCHOS) $(REF_PACK_WATCHOS) $(SDK_PACK_WATCHOS)
pack-macos: $(RUNTIME_PACKS_MACOS) $(REF_PACK_MACOS) $(SDK_PACK_MACOS)

TARGETS += $(RUNTIME_PACKS) $(REF_PACKS) $(SDK_PACKS)

test/NuGet.config: NuGet.config Makefile
	$(Q) $(CP) $< $@.tmp
	$(Q) nuget sources add -Name local-dotnet-feed -Source $(abspath $(DOTNET_FEED_DIR)) -ConfigFile $@.tmp
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

TARGETS += \
	test/NuGet.config \
	test/global.json \

prepare:
	$(Q) V=$(V) \
	TOP=$(TOP) \
	DOTNET_DESTDIR=$(DOTNET_DESTDIR) \
	MAC_DESTDIR=$(MAC_DESTDIR) \
	IOS_DESTDIR=$(IOS_DESTDIR) \
	MAC_FRAMEWORK_DIR=$(MAC_FRAMEWORK_DIR) \
	MONOTOUCH_PREFIX=$(MONOTOUCH_PREFIX) \
	./build-nugets.sh

all-local:: $(TARGETS) targets/Xamarin.Shared.Sdk.Versions.props

clean-local::
	$(Q) rm -Rf $(DOTNET_DESTDIR) $(DOTNET_NUPKG_DIR) $(DOTNET_FEED_DIR)
	$(Q) git clean -xfdq

.SECONDARY: