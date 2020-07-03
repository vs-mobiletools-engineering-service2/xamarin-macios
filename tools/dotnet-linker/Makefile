TOP=../..

include $(TOP)/Make.config
include $(TOP)/mk/rules.mk

BUILD_DIR=bin/Debug/net5.0

DOTNET_TARGETS += \
	$(BUILD_DIR)/dotnet-linker.dll \
	$(foreach platform,$(DOTNET_PLATFORMS),$(DOTNET_DESTDIR)/Microsoft.$(platform).Sdk/tools/dotnet-linker/dotnet-linker.dll) \
	$(foreach platform,$(DOTNET_PLATFORMS),$(DOTNET_DESTDIR)/Microsoft.$(platform).Sdk/tools/dotnet-linker/dotnet-linker.pdb) \
	$(foreach platform,$(DOTNET_PLATFORMS),$(DOTNET_DESTDIR)/Microsoft.$(platform).Sdk/tools/dotnet-linker/registerentryassembly.xml) \
	$(foreach platform,$(DOTNET_PLATFORMS),$(DOTNET_DESTDIR)/Microsoft.$(platform).Sdk/tools/dotnet-linker/uibutton.xml) \

DOTNET_DIRECTORIES += \
	$(foreach platform,$(DOTNET_PLATFORMS),$(DOTNET_DESTDIR)/Microsoft.$(platform).Sdk/tools/dotnet-linker) \

# This is a list of all the build output, with a percentage sign for the last dot in the filename (because that's how you can tell make that a single target has multiple output).
BUILD_OUTPUT = \
	$(BUILD_DIR)/dotnet-linker%dll \
	$(BUILD_DIR)/dotnet-linker%pdb \
	$(BUILD_DIR)/registerentryassembly%xml \
	$(BUILD_DIR)/uibutton%xml \

# dotnet-linker.csproj.inc contains the dotnet_linker_dependencies variable used to determine if mtouch needs to be rebuilt or not.
dotnet-linker.csproj.inc: export BUILD_EXECUTABLE=$(DOTNET5) build
dotnet-linker.csproj.inc: global.json
-include dotnet-linker.csproj.inc

$(BUILD_OUTPUT): Makefile global.json $(dotnet_linker_dependencies)
	$(Q_DOTNET_BUILD) $(DOTNET5) build $(XBUILD_VERBOSITY)

../mtouch/Errors.Designer.cs: # the build breaks without this...

$(DOTNET_DESTDIR)/Microsoft.iOS.Sdk/tools/dotnet-linker/%: $(BUILD_DIR)/% | $(DOTNET_DESTDIR)/Microsoft.iOS.Sdk/tools/dotnet-linker
	$(Q) $(CP) $< $@

$(DOTNET_DESTDIR)/Microsoft.tvOS.Sdk/tools/dotnet-linker/%: $(BUILD_DIR)/% | $(DOTNET_DESTDIR)/Microsoft.tvOS.Sdk/tools/dotnet-linker
	$(Q) $(CP) $< $@

$(DOTNET_DESTDIR)/Microsoft.watchOS.Sdk/tools/dotnet-linker/%: $(BUILD_DIR)/% | $(DOTNET_DESTDIR)/Microsoft.watchOS.Sdk/tools/dotnet-linker
	$(Q) $(CP) $< $@

$(DOTNET_DESTDIR)/Microsoft.macOS.Sdk/tools/dotnet-linker/%: $(BUILD_DIR)/% | $(DOTNET_DESTDIR)/Microsoft.macOS.Sdk/tools/dotnet-linker
	$(Q) $(CP) $< $@

$(DOTNET_DIRECTORIES):
	$(Q) mkdir -p $@

# This tells NuGet to use the exact same dotnet version we've configured in Make.config
global.json: $(TOP)/Make.config.inc Makefile $(TOP)/.git/HEAD $(TOP)/.git/index
	$(Q_GEN) \
		printf "{\n" > $@; \
		printf "\t\"sdk\": { \"version\": \"$(DOTNET5_VERSION)\" }\n" >> $@; \
		printf "}\n" >> $@

all-local:: $(DOTNET_TARGETS)
install-local:: $(DOTNET_TARGETS)
