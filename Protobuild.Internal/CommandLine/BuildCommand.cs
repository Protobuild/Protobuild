using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Protobuild
{
    public class BuildCommand : ICommand
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
            string builderPath = null;
            var extraArgs = string.Empty;

            if (hostPlatform == "Windows")
            {
                // Find latest version of MSBuild.
                var registryKey =
                    Registry.LocalMachine.OpenSubKey("SOFTWARE")?
                        .OpenSubKey("Microsoft")?
                        .OpenSubKey("MSBuild")?
                        .OpenSubKey("ToolsVersions");
                if (registryKey == null)
                {
                    Console.Error.WriteLine(
                        "ERROR: No versions of MSBuild were available " +
                        "according to the registry (or they were not readable).");
                    return 1;
                }

                var subkeys = registryKey.GetSubKeyNames();
                var orderedVersions =
                    subkeys.OrderByDescending(x => int.Parse(x.Split('.').First(), CultureInfo.InvariantCulture));
                builderPath = (from version in orderedVersions
                    let path = (string) registryKey.OpenSubKey(version)?.GetValue("MSBuildToolsPath")
                    where path != null && Directory.Exists(path)
                    let msbuild = Path.Combine(path, "MSBuild.exe")
                    where File.Exists(msbuild)
                    select msbuild).FirstOrDefault();

                if (builderPath == null)
                {
                    Console.Error.WriteLine("ERROR: Unable to find installed MSBuild in any installed tools version.");
                    return 1;
                }

                if (!builderPath.Contains("v2.0.50727"))
                {
                    extraArgs = "/m ";
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
                            builderPath = result;
                            break;
                        }
                    }
                }

                if (builderPath == null)
                {
                    Console.Error.WriteLine("ERROR: Unable to find xbuild on the current PATH.");
                    return 1;
                }
            }

            if (!string.IsNullOrWhiteSpace(execution.BuildTarget))
            {
                extraArgs += "/t:\"" + execution.BuildTarget + "\" ";
            }
            foreach (var prop in execution.BuildProperties)
            {
                extraArgs += "/p:\"" + prop.Key.Replace("\"", "\\\"") + "\"=\"" + (prop.Value ?? string.Empty).Replace("\"", "\\\"") + "\" ";
            }

            Console.WriteLine("INFO: Using " + builderPath + " to perform this build.");

            var targetPlatforms = (execution.Platform ?? hostPlatform).Split(',');
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));

            foreach (var platform in targetPlatforms)
            {
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
    }
}

