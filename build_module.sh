#!/bin/bash

# Build the module
echo "Building CustomModule"

# Compile the project
dotnet build build.proj

# Build the module
dotnet msbuild build.proj -t:build_module

echo "Module built successfully"