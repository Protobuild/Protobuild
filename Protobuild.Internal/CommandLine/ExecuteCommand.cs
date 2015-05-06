using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Diagnostics;

namespace Protobuild
{
    public class ExecuteCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;

        private readonly IHostPlatformDetector m_HostPlatformDetector;

        private readonly IProjectOutputPathCalculator m_ProjectOutputPathCalculator;

        public ExecuteCommand(
            IActionDispatch actionDispatch,
            IHostPlatformDetector hostPlatformDetector,
            IProjectOutputPathCalculator projectOutputPathCalculator)
        {
            this.m_ActionDispatch = actionDispatch;
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_ProjectOutputPathCalculator = projectOutputPathCalculator;
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
            if (!File.Exists(Path.Combine("Build", "Module.xml")))
            {
                throw new InvalidOperationException("No module present.");
            }

            var platform = this.m_HostPlatformDetector.DetectPlatform();
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
            var definitions = module.GetDefinitionsRecursively(platform);
            var target = definitions.FirstOrDefault(x => x.Name == execution.ExecuteProjectName);
            if (target == null)
            {
                Console.WriteLine(
                    "There is no project definition with name '" + execution.ExecuteProjectName + "'");
                return 1;
            }

            string executablePath = null;
            var document = XDocument.Load(target.DefinitionPath);
            var assemblyName = this.m_ProjectOutputPathCalculator.GetProjectAssemblyName(platform, target, document);
            var prefixPath = this.m_ProjectOutputPathCalculator.GetProjectOutputPathPrefix(platform, target, document, false);
            var directory = new DirectoryInfo(prefixPath);
            var subdirectories = directory.GetDirectories();
            var debugDirectory = subdirectories.FirstOrDefault(x => x.Name.ToLowerInvariant() == "debug");
            if (debugDirectory != null)
            {
                executablePath = Path.Combine(debugDirectory.FullName, assemblyName + ".exe");
            }
            else
            {
                var firstDirectory = subdirectories.FirstOrDefault();
                if (firstDirectory != null)
                {
                    executablePath = Path.Combine(firstDirectory.FullName, assemblyName + ".exe");
                }
            }

            if (executablePath == null)
            {
                Console.WriteLine(
                    "There is no output path for '" + execution.ExecuteProjectName + "'; has the project been built?");
                return 1;
            }

            if (!File.Exists(executablePath))
            {
                Console.WriteLine(
                    "There is no executable for '" + execution.ExecuteProjectName + "'; has the project been built?");
                return 1;
            }

            var arguments = execution.ExecuteProjectArguments
                .Select(x => "\"" + x.Replace("\"", "\\\"") + "\"")
                .Aggregate((a, b) => a + " " + b);

            Process process;
            if (Path.DirectorySeparatorChar == '/')
            {
                process = Process.Start(
                    "mono",
                    "--debug \"" + executablePath + "\" " + arguments);
            }
            else
            {
                process = Process.Start(
                    executablePath,
                    arguments);
            }

            process.WaitForExit();
            return process.ExitCode;
        }

        public string GetDescription()
        {
            return @"
Executes the C# project based on it's name (regardless of where
it's located in the module).  This can be used to reliabily
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
    }
}

