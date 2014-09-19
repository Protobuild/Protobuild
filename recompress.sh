#!/bin/bash
echo "Recompressing resources..."

cd Protobuild/bin/Linux/AnyCPU/Debug
mono Protobuild.exe --compress ../../../../BuildResources/GenerateProject.xslt
mono Protobuild.exe --compress ../../../../BuildResources/GenerateSolution.xslt
mono Protobuild.exe --compress ../../../../BuildResources/JSILTemplate.htm
mono Protobuild.exe --compress ../../../../../ProtobuildManager/bin/Linux/AnyCPU/Release/ProtobuildManager.exe
cp ../../../../../ProtobuildManager/bin/Linux/AnyCPU/Release/ProtobuildManager.exe.gz ../../../../BuildResources/ProtobuildManager.exe.gz
