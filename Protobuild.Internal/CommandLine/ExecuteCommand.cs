using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;
using System.Reflection;

namespace Protobuild
{
    internal class ExecuteCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;

        private readonly IHostPlatformDetector m_HostPlatformDetector;

        private readonly IProjectOutputPathCalculator m_ProjectOutputPathCalculator;

        private readonly IPackageGlobalTool m_PackageGlobalTool;

        public ExecuteCommand(
            IActionDispatch actionDispatch,
            IHostPlatformDetector hostPlatformDetector,
            IProjectOutputPathCalculator projectOutputPathCalculator,
            IPackageGlobalTool packageGlobalTool)
        {
            this.m_ActionDispatch = actionDispatch;
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_ProjectOutputPathCalculator = projectOutputPathCalculator;
            this.m_PackageGlobalTool = packageGlobalTool;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            pendingExecution.ExecuteProjectName = args[0];

            var newArgs = new string[args.Length - 1];
            for (var i = 1; i < args.Length; i++)
            {
                newArgs[i - 1] = args[i];
            }

            pendingExecution.ExecuteProjectArguments = newArgs;
        }

        public int Execute(Execution execution)
        {
            var assemblyPath = Assembly.GetEntryAssembly().Location;
            var fileInfo = new FileInfo(assemblyPath);
            var modulePath = fileInfo.DirectoryName;
            if (modulePath == null)
            {
                RedirectableConsole.WriteLine(
                    "Unable to determine the location of Protobuild.");
                return 1;
            }

            if (!File.Exists(Path.Combine(modulePath, "Build", "Module.xml")))
            {
                modulePath = execution.WorkingDirectory;
            }

            string executablePath = null;
            var executableIsNative = false;
            if (!File.Exists(Path.Combine(modulePath, "Build", "Module.xml")))
            {
                // We can only run global tools.
                var globalToolPath = this.m_PackageGlobalTool.ResolveGlobalToolIfPresent(execution.ExecuteProjectName);
                if (globalToolPath == null)
                {
                    RedirectableConsole.WriteLine(
                        "There is no global tool registered as '" + execution.ExecuteProjectName + "'");
                    return 1;
                }
                else
                {
                    executablePath = globalToolPath;
                }
            }
            else
            {
                var platform = this.m_HostPlatformDetector.DetectPlatform();
                var module = ModuleInfo.Load(Path.Combine(modulePath, "Build", "Module.xml"));
                var definitions = module.GetDefinitionsRecursively(platform).ToList();
                var target = definitions.FirstOrDefault(x => x.Name == execution.ExecuteProjectName);
                if (target == null)
                {
                    // Check to see if there is any external project definition that provides the tool.
                    foreach (var definition in definitions)
                    {
                        var document = XDocument.Load(definition.DefinitionPath);
                        if (document.Root.Name.LocalName == "ExternalProject")
                        {
                            foreach (var node in document.Root.Elements().Where(x => x.Name.LocalName == "Tool"))
                            {
                                var name = node.Attribute(XName.Get("Name")).Value;
                                var path = node.Attribute(XName.Get("Path")).Value;

                                if (name == execution.ExecuteProjectName)
                                {
                                    executablePath = Path.Combine(definition.ModulePath, path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
                                    break;
                                }
                            }
                        }
                    }

                    if (executablePath == null)
                    {
                        var globalToolPath = this.m_PackageGlobalTool.ResolveGlobalToolIfPresent(execution.ExecuteProjectName);
                        if (globalToolPath == null)
                        {
                            RedirectableConsole.WriteLine(
                                "There is no project definition with name '" + execution.ExecuteProjectName + "'");
                            return 1;
                        }
                        else
                        {
                            executablePath = globalToolPath;
                        }
                    }
                }
                else
                {
                    var document = XDocument.Load(target.DefinitionPath);
                    if (document.Root.Name.LocalName == "Project")
                    {
                        var assemblyName = this.m_ProjectOutputPathCalculator.GetProjectAssemblyName(platform, target, document);
                        var prefixPath = this.m_ProjectOutputPathCalculator.GetProjectOutputPathPrefix(platform, target, document, false);
                        var directory = new DirectoryInfo(Path.Combine(modulePath, prefixPath));
                        var subdirectories = directory.GetDirectories();
                        var preferredName = (execution.ExecuteProjectConfiguration ?? "Debug").ToLowerInvariant();
                        var preferredDirectory = subdirectories.FirstOrDefault(x => x.Name.ToLowerInvariant() == preferredName);
                        var targetName = assemblyName + ".exe";
                        string targetAppName = null;
                        if (m_HostPlatformDetector.DetectPlatform() == "MacOS")
                        {
                            targetAppName = assemblyName + ".app/Contents/MacOS/" + assemblyName;
                        }
                        if (preferredDirectory != null && (File.Exists(Path.Combine(preferredDirectory.FullName, targetName)) || (targetAppName != null && File.Exists(Path.Combine(preferredDirectory.FullName, targetAppName)))))
                        {
                            if (targetAppName != null && File.Exists(Path.Combine(preferredDirectory.FullName, targetAppName)))
                            {
                                // We must use the 'defaults' tool on macOS to determine if the target is a
                                // graphical application.  We do this by checking for the presence of NSPrincipalClass
                                // in the Info.plist file, which must be present for graphical applications to
                                // work.  If it does not exist, we assume it is a command-line application and run
                                // it normally like on any other platform.  We have to do this because the native
                                // entry point is the only one that has access to the GUI, while the non-native
                                // entry point is the only one that has the correct working directory set on
                                // startup.
                                if (TargetMacOSAppIsGraphical(Path.Combine(preferredDirectory.FullName, assemblyName + ".app")))
                                {
                                    executablePath = Path.Combine(preferredDirectory.FullName, targetAppName);
                                    executableIsNative = true;
                                }
                            }
                            if (executablePath == null && File.Exists(Path.Combine(preferredDirectory.FullName, targetName)))
                            {
                                executablePath = Path.Combine(preferredDirectory.FullName, targetName);
                            }
                        }
                        else
                        {
                            foreach (var subdirectory in subdirectories)
                            {
                                var tempPath = Path.Combine(subdirectory.FullName, assemblyName + ".exe");
                                string tempAppName = null;
                                if (m_HostPlatformDetector.DetectPlatform() == "MacOS")
                                {
                                    tempAppName = Path.Combine(subdirectory.FullName, assemblyName + ".app/Contents/MacOS/" + assemblyName);
                                }

                                if (tempAppName != null && File.Exists(tempAppName))
                                {
                                    // We must use the 'defaults' tool on macOS to determine if the target is a
                                    // graphical application.  We do this by checking for the presence of NSPrincipalClass
                                    // in the Info.plist file, which must be present for graphical applications to
                                    // work.  If it does not exist, we assume it is a command-line application and run
                                    // it normally like on any other platform.  We have to do this because the native
                                    // entry point is the only one that has access to the GUI, while the non-native
                                    // entry point is the only one that has the correct working directory set on
                                    // startup.
                                    if (TargetMacOSAppIsGraphical(Path.Combine(subdirectory.FullName, assemblyName + ".app")))
                                    {
                                        executablePath = tempAppName;
                                        executableIsNative = true;
                                        break;
                                    }
                                }
                                if (File.Exists(tempPath))
                                {
                                    executablePath = tempPath;
                                    break;
                                }
                            }
                        }
                    }
                    else if (document.Root.Name.LocalName == "ExternalProject")
                    {
                        foreach (var node in document.Root.Elements().Where(x => x.Name.LocalName == "Tool"))
                        {
                            var name = node.Attribute(XName.Get("Name")).Value;
                            var path = node.Attribute(XName.Get("Path")).Value;

                            if (name == execution.ExecuteProjectName)
                            {
                                executablePath = Path.Combine(target.ModulePath, path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));
                                break;
                            }
                        }
                    }

                    if (executablePath == null)
                    {
                        var globalToolPath = this.m_PackageGlobalTool.ResolveGlobalToolIfPresent(execution.ExecuteProjectName);
                        if (globalToolPath == null)
                        {
                            RedirectableConsole.WriteLine(
                                "There is no output path for '" + execution.ExecuteProjectName + "'; has the project been built?");
                            return 1;
                        }
                        else
                        {
                            executablePath = globalToolPath;
                        }
                    }
                }
            }

            if (!File.Exists(executablePath))
            {
                RedirectableConsole.WriteLine(
                    "There is no executable for '" + execution.ExecuteProjectName + "'; has the project been built?");
                return 1;
            }

            var arguments = string.Empty;
            if (execution.ExecuteProjectArguments.Length > 0)
            {
                arguments = execution.ExecuteProjectArguments
                    .Select(x => "\"" + x.Replace("\"", "\\\"") + "\"")
                    .Aggregate((a, b) => a + " " + b);
            }

            ProcessStartInfo startInfo;
            if (Path.DirectorySeparatorChar == '/' && !executableIsNative)
            {
                var mono = "mono";
                if (m_HostPlatformDetector.DetectPlatform() == "MacOS" && File.Exists("/usr/local/bin/mono"))
                {
                    // After upgrading to OSX El Capitan, the /usr/local/bin folder is no longer in
                    // the system PATH.  If we can't find Mono with the which tool, manually set the
                    // path here in an attempt to find it.
                    mono = "/usr/local/bin/mono";
                }

                startInfo = new ProcessStartInfo(
                    mono,
                    "--debug \"" + executablePath + "\" " + arguments);
            }
            else
            {
                startInfo = new ProcessStartInfo(
                    executablePath,
                    arguments);
            }
            startInfo.UseShellExecute = false;

            var process = Process.Start(startInfo);
            process.WaitForExit();
            return process.ExitCode;
        }

        private bool TargetMacOSAppIsGraphical(string appFolderPath)
        {
            var plistPath = Path.GetFullPath(Path.Combine(appFolderPath, "Contents/Info"));

            var startInfo = new ProcessStartInfo(
                "/usr/bin/defaults",
                "read \"" + plistPath + "\" NSPrincipalClass");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            var process = Process.Start(startInfo);
            if (process == null)
            {
                // Unable to run defaults; assume non-native.
                return false;
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                // NSPrincipalClass is present; this is a graphical app.
                return true;
            }

            return false;
        }

        public string GetDescription()
        {
            return @"
Executes the C# project based on it's name (regardless of where
it's located in the module), or the global tool installed with
the given name.  This can be used to reliably
execute tools such as unit testing frameworks.  The first
argument is the name of the project; any other arguments are
passed through to the executable.  Therefore, this is the last
argument that Protobuild will parse (all arguments after the
project name will be passed to the executable and not parsed
by Protobuild).
";
        }

        public int GetArgCount()
        {
            return -1;
        }

        public string[] GetArgNames()
        {
            return new[] { "name", "arg1", "arg2", "...", "argN" };
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

