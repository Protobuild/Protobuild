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

if [ "$1" == "--nogen" ]; then
  NOGEN="true"
else
  NOGEN="false"
fi

echo "Generating project and performing first-pass build..."
if [ "$NOGEN" == "true" ]; then
  mono Protobuild.exe --sync $PLATFORM
else
  mono Protobuild.exe --resync $PLATFORM
fi
xbuild /p:Configuration=Release /t:Rebuild Protobuild.$PLATFORM.sln

echo "Compressing resources..."
PROTOBUILD_COMPRESS=Protobuild.Compress/bin/$PLATFORM/AnyCPU/Release/Protobuild.Compress.exe
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/GenerateProject.CSharp.xslt Protobuild.Internal/BuildResources/GenerateProject.CSharp.xslt.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/GenerateSolution.xslt Protobuild.Internal/BuildResources/GenerateSolution.xslt.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/SelectSolution.xslt Protobuild.Internal/BuildResources/SelectSolution.xslt.lzma
mono $PROTOBUILD_COMPRESS Protobuild.Internal/BuildResources/JSILTemplate.htm Protobuild.Internal/BuildResources/JSILTemplate.htm.lzma

echo "Performing second-pass build..."
xbuild /p:Configuration=Release /t:Rebuild Protobuild.$PLATFORM.sln

echo "Compressing Protobuild.Internal..."
mono $PROTOBUILD_COMPRESS Protobuild.Internal/bin/$PLATFORM/AnyCPU/Release/Protobuild.Internal.dll Protobuild/Protobuild.Internal.dll.lzma

echo "Performing final-pass build..."
xbuild /p:Configuration=Release /t:Rebuild Protobuild.$PLATFORM.sln

echo "Running tests..."
mono --debug packages/xunit.runners.1.9.2/tools/xunit.console.clr4.exe Protobuild.Tests/bin/$PLATFORM/AnyCPU/Release/Protobuild.Tests.dll /noshadow

echo "Copying built Protobuild to root of repository..."
cp Protobuild/bin/$PLATFORM/AnyCPU/Release/Protobuild.exe ./
