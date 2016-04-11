using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Protobuild
{
    internal class BuildCommand : ICommand
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;

        public BuildCommand(IHostPlatformDetector hostPlatformDetector)
        {
            _hostPlatformDetector = hostPlatformDetector;
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

            if (hostPlatform == "Windows")
            {
                foreach (var arch in new[] {RegistryView.Default, RegistryView.Registry32, RegistryView.Registry64})
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

                        Console.Error.WriteLine(
                            "ERROR: No versions of MSBuild were available " +
                            "according to the registry (or they were not readable).");
                        return 1;
                    }

                    var subkeys = registryKey.GetSubKeyNames();
                    var orderedVersions =
                        subkeys.OrderByDescending(x => int.Parse(x.Split('.').First(), CultureInfo.InvariantCulture));
                    var builderPath = (from version in orderedVersions
                        let path = (string) registryKey.OpenSubKey(version)?.GetValue("MSBuildToolsPath")
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

                        Console.Error.WriteLine(
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
            else
            {
                // Find path to xbuild.
                var whichPaths = new[] {"/bin/which", "/usr/bin/which"};
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

                if (builderPathNativeArch == null && _hostPlatformDetector.DetectPlatform() == "MacOS" && File.Exists("/usr/local/bin/xbuild"))
                {
                    // After upgrading to OSX El Capitan, the /usr/local/bin folder is no longer in
                    // the system PATH.  If we can't find xbuild with the which tool, manually set the
                    // path here in an attempt to find it.
                    builderPathNativeArch = "/usr/local/bin/xbuild";
                }

                if (builderPathNativeArch == null)
                {
                    Console.Error.WriteLine("ERROR: Unable to find xbuild on the current PATH.");
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
                    Console.WriteLine("INFO: Using " + builderPath32 + " (forced 32-bit) to perform this build.");
                    break;
                case "x64":
                    Console.WriteLine("INFO: Using " + builderPath64 + " (forced 64-bit) to perform this build.");
                    break;
                case "Default":
                default:
                    Console.WriteLine("INFO: Using " + builderPathNativeArch + " (32-bit: " + builderPath32 + ") to perform this build.");
                    break;
            }

            var targetPlatforms = (execution.Platform ?? hostPlatform).Split(',');
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));

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

                Console.WriteLine("INFO: Executing " + builderPath + " with arguments: " + extraArgs + fileToBuild);

                var process =
                    Process.Start(new ProcessStartInfo(builderPath, extraArgs + fileToBuild) {UseShellExecute = false});
                if (process == null)
                {
                    Console.Error.WriteLine("ERROR: Build process did not start successfully.");
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

