#!/bin/bash

set -e
set -x

if [[ `uname` == Darwin ]]; then
  PLATFORM=MacOS
else
  PLATFORM=Linux
fi

mono Protobuild.exe --generate $PLATFORM
xbuild /p:Configuration=Debug Protobuild.$PLATFORM.sln
xbuild /p:Configuration=Release Protobuild.$PLATFORM.sln
./recompress.sh
xbuild /p:Configuration=Release Protobuild.$PLATFORM.sln
cp Protobuild/bin/$PLATFORM/AnyCPU/Release/Protobuild.exe ./
mono Protobuild.exe --generate $PLATFORM
