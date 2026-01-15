#!/bin/bash
set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Build the module
echo -e "${GREEN}Building Zitac VMware Module${NC}"

# Compile the project
echo -e "${YELLOW}Compiling the project...${NC}"
dotnet build build.proj

if [ $? -ne 0 ]; then
    echo -e "${RED}Build failed!${NC}"
    exit 1
fi

# Build the module using CreateDecisionsModule
echo -e "${YELLOW}Creating Decisions module package...${NC}"
dotnet msbuild build.proj -t:build_module

if [ $? -ne 0 ]; then
    echo -e "${RED}Module packaging failed!${NC}"
    exit 1
fi

echo -e "${GREEN}Module built successfully!${NC}"
echo -e "${CYAN}Output: Zitac.VMware.zip${NC}"