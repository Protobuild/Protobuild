#!/bin/bash

set -e
set -x

mono Protobuild.exe --generate
xbuild /p:Configuration=Debug Protobuild.Linux.sln
./recompress.sh
xbuild /p:Configuration=Release Protobuild.Linux.sln
cp Protobuild/bin/Linux/AnyCPU/Release/Protobuild.exe ./
mono Protobuild.exe --generate
