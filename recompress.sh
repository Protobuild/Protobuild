#!/bin/bash
set -x

if [[ `uname` == Darwin ]]; then
  PLATFORM=MacOS
else
  PLATFORM=Linux
fi

echo "Recompressing resources..."

cd Protobuild/bin/$PLATFORM/AnyCPU/Debug
mono Protobuild.exe --compress ../../../../BuildResources/GenerateProject.xslt
mono Protobuild.exe --compress ../../../../BuildResources/GenerateSolution.xslt
mono Protobuild.exe --compress ../../../../BuildResources/SelectSolution.xslt
mono Protobuild.exe --compress ../../../../BuildResources/JSILTemplate.htm
mono Protobuild.exe --compress ../../../../../ProtobuildManager/bin/$PLATFORM/AnyCPU/Release/ProtobuildManager.exe
cp ../../../../../ProtobuildManager/bin/$PLATFORM/AnyCPU/Release/ProtobuildManager.exe.gz ../../../../BuildResources/ProtobuildManager.exe.gz
