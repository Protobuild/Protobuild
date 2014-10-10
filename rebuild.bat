Protobuild.exe --generate Windows
C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /t:Rebuild /p:Configuration=Debug Protobuild.Windows.sln
call recompress.bat
cd %~dp0
C:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /t:Rebuild /p:Configuration=Release Protobuild.Windows.sln
xcopy /Y /F Protobuild\bin\Windows\AnyCPU\Release\Protobuild.exe Protobuild.exe
Protobuild.exe --generate Windows
