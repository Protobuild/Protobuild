param(
    [switch] [bool] $NoGen,
    [switch] [bool] $NoTest)

$ErrorActionPreference = 'Stop'

if ($PSScriptRoot -ne $null) {
    cd $PSScriptRoot\..
} else {
    Write-Warning "Unable to change directory!"
}

$PLATFORM="Windows"

echo "Generating project and performing first-pass build..."
if ($NoGen) {
    .\Protobuild.exe --sync $PLATFORM
} else {
    .\Protobuild.exe --resync $PLATFORM
}
.\Protobuild.exe --build $PLATFORM --build-target Rebuild --build-property Configuration Release
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Compressing resources..."
.\Protobuild.exe --execute-configuration Release --execute Protobuild.Compress Protobuild.Internal\BuildResources\GenerateProject.CSharp.xslt Protobuild.Internal\BuildResources\GenerateProject.CSharp.xslt.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}
.\Protobuild.exe --execute-configuration Release --execute Protobuild.Compress Protobuild.Internal\BuildResources\GenerateProject.CPlusPlus.VisualStudio.xslt Protobuild.Internal\BuildResources\GenerateProject.CPlusPlus.VisualStudio.xslt.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}
.\Protobuild.exe --execute-configuration Release --execute Protobuild.Compress Protobuild.Internal\BuildResources\GenerateSolution.xslt Protobuild.Internal\BuildResources\GenerateSolution.xslt.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}
.\Protobuild.exe --execute-configuration Release --execute Protobuild.Compress Protobuild.Internal\BuildResources\SelectSolution.xslt Protobuild.Internal\BuildResources\SelectSolution.xslt.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}
.\Protobuild.exe --execute-configuration Release --execute Protobuild.Compress Protobuild.Internal\BuildResources\JSILTemplate.htm Protobuild.Internal\BuildResources\JSILTemplate.htm.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}
.\Protobuild.exe --execute-configuration Release --execute Protobuild.Compress Protobuild.Internal\BuildResources\GenerationFunctions.cs Protobuild.Internal\BuildResources\GenerationFunctions.cs-msbuild-hack.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Performing second-pass build..."
.\Protobuild.exe --build $PLATFORM --build-target Rebuild --build-property Configuration Release
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Compressing Protobuild.Internal..."
.\Protobuild.exe --execute-configuration Release --execute Protobuild.Compress Protobuild.Internal\bin\$PLATFORM\AnyCPU\Release\Protobuild.Internal.dll Protobuild\Protobuild.Internal.dll.lzma
if ($LASTEXITCODE -ne 0) {
    exit 1
}

echo "Performing final-pass build..."
.\Protobuild.exe --build $PLATFORM --build-target Rebuild --build-property Configuration Release
if ($LASTEXITCODE -ne 0) {
    exit 1
}

if (!$NoTest) {
    echo "Running tests..."
    .\Protobuild.exe --execute-configuration Release --execute Protobuild.UnitTests
    if ($LASTEXITCODE -ne 0) {
        echo "One or more unit tests failed."
        exit 1
    }
    .\Protobuild.exe --execute-configuration Release --execute Protobuild.FunctionalTests
    if ($LASTEXITCODE -ne 0) {
        echo "One or more functional tests failed."
        exit 1
    }
}

echo "Copying built Protobuild to root of repository..."
copy-item -Force Protobuild\bin\$PLATFORM\AnyCPU\Release\Protobuild.exe .\Protobuild.exe
