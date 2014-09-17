echo "Recompressing resources..."

cd %~dp0
cd Protobuild\bin\Windows\AnyCPU\Release
Protobuild.exe --compress ..\..\..\..\BuildResources\GenerateProject.xslt
Protobuild.exe --compress ..\..\..\..\BuildResources\GenerateSolution.xslt
Protobuild.exe --compress ..\..\..\..\BuildResources\JSILTemplate.htm
Protobuild.exe --compress ..\..\..\..\..\ProtobuildManager\bin\Release\ProtobuildManager.exe
xcopy /Y ..\..\..\ProtobuildManager\bin\Release\ProtobuildManager.exe.gz ..\..\BuildResources\ProtobuildManager.exe.gz