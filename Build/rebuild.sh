#!/bin/bash

set -e
set -x

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd $DIR/..

if [[ `uname` == Darwin ]]; then
  PLATFORM=MacOS
else
  PLATFORM=Linux
fi

if [ "$1" == "--nogen" ] || [ "$2" == "--nogen" ] || [ "$3" == "--nogen" ]; then
  NOGEN="true"
else
  NOGEN="false"
fi

if [ "$1" == "--notest" ] || [ "$2" == "--notest" ] || [ "$3" == "--notest" ]; then
  NOTEST="true"
else
  NOTEST="false"
fi

CONFIGURATION="Release"
if [ "$1" == "--debug" ] || [ "$2" == "--debug" ] || [ "$3" == "--debug" ]; then
  CONFIGURATION="Debug"
fi

echo "Generating project and performing first-pass build..."
if [ "$NOGEN" == "true" ]; then
  mono Protobuild.exe --sync $PLATFORM
else
  mono Protobuild.exe --resync $PLATFORM
fi
xbuild /p:Configuration=$CONFIGURATION /t:Rebuild Protobuild.$PLATFORM.sln

echo "Compressing resources..."
PROTOBUILD_COMPRESS=Protobuild.Compress/bin/$PLATFORM/AnyCPU/$CONFIGURATION/Protobuild.Compress.exe
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/GenerateProject.CSharp.xslt Protobuild.Internal/BuildResources/GenerateProject.CSharp.xslt.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/GenerateProject.CPlusPlus.VisualStudio.xslt Protobuild.Internal/BuildResources/GenerateProject.CPlusPlus.VisualStudio.xslt.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/GenerateProject.CPlusPlus.MonoDevelop.xslt Protobuild.Internal/BuildResources/GenerateProject.CPlusPlus.MonoDevelop.xslt.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/GenerateSolution.xslt Protobuild.Internal/BuildResources/GenerateSolution.xslt.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/GenerationFunctions.cs Protobuild.Internal/BuildResources/GenerationFunctions.cs-msbuild-hack.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/SelectSolution.xslt Protobuild.Internal/BuildResources/SelectSolution.xslt.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/JSILTemplate.htm Protobuild.Internal/BuildResources/JSILTemplate.htm.lzma

echo "Performing second-pass build..."
xbuild /p:Configuration=$CONFIGURATION /t:Rebuild Protobuild.$PLATFORM.sln

echo "Compressing Protobuild.Internal..."
mono $PROTOBUILD_COMPRESS Protobuild.Internal/bin/$PLATFORM/AnyCPU/$CONFIGURATION/Protobuild.Internal.dll Protobuild/Protobuild.Internal.dll.lzma

echo "Performing final-pass build..."
xbuild /p:Configuration=$CONFIGURATION /t:Rebuild Protobuild.$PLATFORM.sln

if [ "$NOTEST" == "false" ]; then
  echo "Running tests..."
  mono Protobuild.exe --execute-configuration $CONFIGURATION --execute Protobuild.UnitTests
  mono Protobuild.exe --execute-configuration $CONFIGURATION --execute Protobuild.FunctionalTests
fi

if [ "$CONFIGURATION" == "Release" ]; then
  echo "Copying built Protobuild to root of repository..."
  cp Protobuild/bin/$PLATFORM/AnyCPU/$CONFIGURATION/Protobuild.exe ./
else
  echo "Will not copy Debug build to root of repository."
fi
