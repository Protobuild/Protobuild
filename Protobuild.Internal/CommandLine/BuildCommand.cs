using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Collections.Generic;

namespace Protobuild
{
    internal class BuildCommand : ICommand
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;
        private readonly IKnownToolProvider _knownToolProvider;

        public BuildCommand(
            IHostPlatformDetector hostPlatformDetector,
            IKnownToolProvider knownToolProvider)
        {
            _hostPlatformDetector = hostPlatformDetector;
            _knownToolProvider = knownToolProvider;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);
            
            if (args.Length > 0)
            {
                pendingExecution.Platform = args[0];
            }
        }

        public int Execute(Execution execution)
        {
            var hostPlatform = _hostPlatformDetector.DetectPlatform();
            string builderPathNativeArch = null;
            string builderPath64 = null;
            string builderPath32 = null;
            var extraArgsNativeArch = string.Empty;
            var extraArgs64 = string.Empty;
            var extraArgs32 = string.Empty;
            var extraArgsGeneral = string.Empty;

            var targetPlatforms = (execution.Platform ?? hostPlatform).Split(',');
            var module = ModuleInfo.Load(Path.Combine(execution.WorkingDirectory, "Build", "Module.xml"));

            if (hostPlatform == "Windows")
            {
                // Newer installs of Visual Studio (like 2017) don't create registry entries for MSBuild, so we have to
                // use a tool called vswhere in order to find MSBuild on these systems.  This call will implicitly install
                // the vswhere package if it's not already installed.
                var vswhere = _knownToolProvider.GetToolExecutablePath("vswhere");
                List<string> installations = null;
                if (vswhere != null && File.Exists(vswhere))
                {
                    try
                    {
                        var processStartInfo = new ProcessStartInfo();
                        processStartInfo.FileName = vswhere;
                        processStartInfo.Arguments = "-products * -requires Microsoft.Component.MSBuild -property installationPath";
                        processStartInfo.UseShellExecute = false;
                        processStartInfo.RedirectStandardOutput = true;
                        var process = Process.Start(processStartInfo);
                        var installationsString = process.StandardOutput.ReadToEnd();
                        installations = installationsString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            RedirectableConsole.ErrorWriteLine("Unable to locate Visual Studio 2017 and later installations (non-zero exit code from vswhere)");
                        }
                    }
                    catch (Exception ex)
                    {
                        RedirectableConsole.ErrorWriteLine("Unable to locate Visual Studio 2017 and later installations: " + ex.Message);
                    }
                }

                if (installations != null)
                {
                    // Check if MSBuild is present in any of those installation paths.
                    foreach (var basePath in installations)
                    {
                        var msbuildLocation = Path.Combine(basePath, "MSBuild\\15.0\\Bin\\MSBuild.exe");
                        if (File.Exists(msbuildLocation))
                        {
                            builderPathNativeArch = msbuildLocation;
                            extraArgsNativeArch = "/m /nodeReuse:false ";
                            builderPath32 = msbuildLocation;
                            extraArgs32 = "/m /nodeReuse:false ";

                            var x64Location = Path.Combine(basePath, "MSBuild\\15.0\\Bin\\amd64\\MSBuild.exe");
                            if (File.Exists(x64Location))
                            {
                                builderPath64 = x64Location;
                                extraArgs64 = "/m /nodeReuse:false ";
                            }

                            break;
                        }
                    }
                }
                
                if (builderPathNativeArch == null)
                {
                    // Try to find via the registry.
                    foreach (var arch in new[] { RegistryView.Default, RegistryView.Registry32, RegistryView.Registry64 })
                    {
                        // Find latest version of MSBuild.
                        var registryKey =
                            RegistryKey.OpenBaseKey(
                                RegistryHive.LocalMachine,
                                arch)
                                .OpenSubKey("SOFTWARE")?
                                .OpenSubKey("Microsoft")?
                                .OpenSubKey("MSBuild")?
                                .OpenSubKey("ToolsVersions");
                        if (registryKey == null)
                        {
                            if (arch == RegistryView.Registry64)
                            {
                                continue;
                            }

                            RedirectableConsole.ErrorWriteLine(
                                "ERROR: No versions of MSBuild were available " +
                                "according to the registry (or they were not readable).");
                            return 1;
                        }

                        var subkeys = registryKey.GetSubKeyNames();
                        var orderedVersions =
                            subkeys.OrderByDescending(x => int.Parse(x.Split('.').First(), CultureInfo.InvariantCulture));
                        var builderPath = (from version in orderedVersions
                                           let path = (string)registryKey.OpenSubKey(version)?.GetValue("MSBuildToolsPath")
                                           where path != null && Directory.Exists(path)
                                           let msbuild = Path.Combine(path, "MSBuild.exe")
                                           where File.Exists(msbuild)
                                           select msbuild).FirstOrDefault();

                        if (builderPath == null)
                        {
                            if (arch == RegistryView.Registry64)
                            {
                                continue;
                            }

                            RedirectableConsole.ErrorWriteLine(
                                "ERROR: Unable to find installed MSBuild in any installed tools version.");
                            return 1;
                        }

                        var extraArgs = string.Empty;
                        if (!builderPath.Contains("v2.0.50727"))
                        {
                            extraArgs = "/m /nodeReuse:false ";
                        }

                        switch (arch)
                        {
                            case RegistryView.Default:
                                builderPathNativeArch = builderPath;
                                extraArgsNativeArch = extraArgs;
                                break;
                            case RegistryView.Registry32:
                                builderPath32 = builderPath;
                                extraArgs32 = extraArgs;
                                break;
                            case RegistryView.Registry64:
                                builderPath64 = builderPath;
                                extraArgs64 = extraArgs;
                                break;
                        }
                    }
                }
            }
            else
            {
                // Find path to xbuild.
                var whichPaths = new[] {"/bin/which", "/usr/bin/which"};

                // We can only use the new MSBuild tool if no projects are C++ projects on Mac or Linux.
                var isAnyNativeProject = false;
                foreach (var def in module.GetDefinitionsRecursively())
                {
                    var document = XDocument.Load(def.DefinitionPath);
                    var languageAttr = document?.Root?.Attributes()?.FirstOrDefault(x => x.Name.LocalName == "Language");
                    if (languageAttr != null && languageAttr.Value == "C++")
                    {
                        isAnyNativeProject = true;
                        break;
                    }
                }
                if (!isAnyNativeProject)
                {
                    foreach (var w in whichPaths)
                    {
                        if (File.Exists(w))
                        {
                            var whichProcess = Process.Start(new ProcessStartInfo(w, "msbuild")
                            {
                                RedirectStandardOutput = true,
                                UseShellExecute = false
                            });
                            if (whichProcess == null)
                            {
                                continue;
                            }
                            var result = whichProcess.StandardOutput.ReadToEnd().Trim();
                            if (!string.IsNullOrWhiteSpace(result) && File.Exists(result))
                            {
                                builderPathNativeArch = result;
                                break;
                            }
                        }
                    }
                }

                if (builderPathNativeArch == null)
                {
                    foreach (var w in whichPaths)
                    {
                        if (File.Exists(w))
                        {
                            var whichProcess = Process.Start(new ProcessStartInfo(w, "xbuild")
                            {
                                RedirectStandardOutput = true,
                                UseShellExecute = false
                            });
                            if (whichProcess == null)
                            {
                                continue;
                            }
                            var result = whichProcess.StandardOutput.ReadToEnd().Trim();
                            if (!string.IsNullOrWhiteSpace(result) && File.Exists(result))
                            {
                                builderPathNativeArch = result;
                                break;
                            }
                        }
                    }
                }

                if (builderPathNativeArch == null && _hostPlatformDetector.DetectPlatform() == "MacOS" && File.Exists("/usr/local/bin/xbuild"))
                {
                    // After upgrading to OSX El Capitan, the /usr/local/bin folder is no longer in
                    // the system PATH.  If we can't find xbuild with the which tool, manually set the
                    // path here in an attempt to find it.
                    builderPathNativeArch = "/usr/local/bin/xbuild";
                }

                if (builderPathNativeArch == null)
                {
                    RedirectableConsole.ErrorWriteLine("ERROR: Unable to find msbuild or xbuild on the current PATH.");
                    return 1;
                }

                builderPath32 = builderPathNativeArch;
                builderPath64 = builderPathNativeArch;
            }

            if (!string.IsNullOrWhiteSpace(execution.BuildTarget))
            {
                extraArgsGeneral += "/t:\"" + execution.BuildTarget + "\" ";
            }
            foreach (var prop in execution.BuildProperties)
            {
                extraArgsGeneral += "/p:\"" + prop.Key.Replace("\"", "\\\"") + "\"=\"" + (prop.Value ?? string.Empty).Replace("\"", "\\\"") + "\" ";
            }

            switch (execution.BuildProcessArchitecture)
            {
                case "x86":
                    RedirectableConsole.WriteLine("INFO: Using " + builderPath32 + " (forced 32-bit) to perform this build.");
                    break;
                case "x64":
                    RedirectableConsole.WriteLine("INFO: Using " + builderPath64 + " (forced 64-bit) to perform this build.");
                    break;
                case "Default":
                default:
                    RedirectableConsole.WriteLine("INFO: Using " + builderPathNativeArch + " (32-bit: " + builderPath32 + ") to perform this build.");
                    break;
            }

            foreach (var platform in targetPlatforms)
            {
                string builderPath;
                string extraArgs;
                
                switch (execution.BuildProcessArchitecture)
                {
                    case "x86":
                        builderPath = builderPath32;
                        extraArgs = extraArgs32 + extraArgsGeneral;
                        break;
                    case "x64":
                        builderPath = builderPath64;
                        extraArgs = extraArgs64 + extraArgsGeneral;
                        break;
                    case "Default":
                    default:
                        builderPath = platform == "WindowsPhone" ? builderPath32 : builderPathNativeArch;
                        extraArgs = (platform == "WindowsPhone" ? extraArgs32 : extraArgsNativeArch) + extraArgsGeneral;
                        break;
                }

                var fileToBuild = module.Name + "." + platform + ".sln";

                RedirectableConsole.WriteLine("INFO: Executing " + builderPath + " with arguments: " + extraArgs + fileToBuild);

                var process =
                    Process.Start(new ProcessStartInfo(builderPath, extraArgs + fileToBuild) {UseShellExecute = false});
                if (process == null)
                {
                    RedirectableConsole.ErrorWriteLine("ERROR: Build process did not start successfully.");
                    return 1;
                }
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return process.ExitCode;
                }
            }

            return 0;
        }

        public string GetDescription()
        {
            return @"
Builds the module for the specified platform (assumes you have
generated the project files first).  If no platform is specified,
builds for the host platform.";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "platform?" };
        }

        public bool IsInternal()
        {
            return false;
        }

        public bool IsRecognised()
        {
            return true;
        }

        public bool IsIgnored()
        {
            return false;
        }
    }
}

