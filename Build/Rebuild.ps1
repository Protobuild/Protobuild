param(
    [switch] [bool] $NoGen)

$ErrorActionPreference = 'Stop'

cd $PSScriptRoot\..

$PLATFORM="Windows"
$msbuild = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

echo "Generating project and performing first-pass build..."
if ($NoGen) {
    .\Protobuild.exe --sync $PLATFORM
} else {
    .\Protobuild.exe --resync $PLATFORM
}
& $msbuild /p:Configuration=Release /t:Rebuild Protobuild.$PLATFORM.sln
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Compressing resources..."
$PROTOBUILD_COMPRESS=".\Protobuild.Compress\bin\$PLATFORM\AnyCPU\Release\Protobuild.Compress.exe"
& $PROTOBUILD_COMPRESS Protobuild.Internal\BuildResources\GenerateProject.CSharp.xslt Protobuild.Internal\BuildResources\GenerateProject.CSharp.xslt.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}
& $PROTOBUILD_COMPRESS Protobuild.Internal\BuildResources\GenerateSolution.xslt Protobuild.Internal\BuildResources\GenerateSolution.xslt.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}
& $PROTOBUILD_COMPRESS Protobuild.Internal\BuildResources\SelectSolution.xslt Protobuild.Internal\BuildResources\SelectSolution.xslt.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}
& $PROTOBUILD_COMPRESS Protobuild.Internal\BuildResources\JSILTemplate.htm Protobuild.Internal\BuildResources\JSILTemplate.htm.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Performing second-pass build..."
& $msbuild /p:Configuration=Release /t:Rebuild Protobuild.$PLATFORM.sln
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Compressing Protobuild.Internal..."
& $PROTOBUILD_COMPRESS Protobuild.Internal\bin\$PLATFORM\AnyCPU\Release\Protobuild.Internal.dll Protobuild\Protobuild.Internal.dll.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Performing final-pass build..."
& $msbuild /p:Configuration=Release /t:Rebuild Protobuild.$PLATFORM.sln
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Running tests..."
.\packages\xunit.runners.2.0.0-rc1-build2826\tools\xunit.console.exe Protobuild.Tests\bin\$PLATFORM\AnyCPU\Release\Protobuild.Tests.dll -noshadow

echo "Copying built Protobuild to root of repository..."
copy-item -Force Protobuild\bin\$PLATFORM\AnyCPU\Release\Protobuild.exe .\Protobuild.exe