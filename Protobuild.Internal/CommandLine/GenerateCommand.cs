using System;
using System.IO;

namespace Protobuild
{
    internal class GenerateCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;

        public GenerateCommand(IActionDispatch actionDispatch)
        {
            this.m_ActionDispatch = actionDispatch;
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
			if (Directory.Exists(Path.Combine(execution.WorkingDirectory, "Build")))
            {
                var module = ModuleInfo.Load(Path.Combine(execution.WorkingDirectory, "Build", "Module.xml"));
                return this.m_ActionDispatch.PerformAction(
                    execution.WorkingDirectory,
                    module,
                    "generate",
                    execution.Platform,
                    execution.EnabledServices.ToArray(),
                    execution.DisabledServices.ToArray(),
                    execution.ServiceSpecificationPath,
                    execution.DebugServiceResolution,
                    execution.DisablePackageResolution,
                    execution.DisableHostProjectGeneration,
                    execution.UseTaskParallelisation,
                    execution.SafePackageResolution,
                    execution.DebugProjectGeneration)
                    ? 0
                    : 1;
            }

            return 1;
        }

        public string GetShortCategory()
        {
            return "Project generation";
        }

        public string GetShortDescription()
        {
            return "generate project and solution files";
        }

        public string GetDescription()
        {
            return @"
Generates the project files from the definitions.  If no
platform is specified, generates for the current platform.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetShortArgNames()
        {
            return GetArgNames();
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

