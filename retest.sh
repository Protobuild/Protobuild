#!/bin/bash

set -e
set -x

if [[ `uname` == Darwin ]]; then
  PLATFORM=MacOS
else
  PLATFORM=Linux
fi

mono Protobuild.exe --sync $PLATFORM
./rebuild.sh
mono --debug packages/xunit.runners.1.9.2/tools/xunit.console.clr4.exe Protobuild.Tests/bin/Linux/AnyCPU/Debug/Protobuild.Tests.dll /noshadow

