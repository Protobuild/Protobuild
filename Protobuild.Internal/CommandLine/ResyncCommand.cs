using System;
using System.IO;

namespace Protobuild
{
    internal class ResyncCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;

        public ResyncCommand(IActionDispatch actionDispatch)
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
                    "resync",
                    execution.Platform,
                    execution.EnabledServices.ToArray(),
                    execution.DisabledServices.ToArray(),
                    execution.ServiceSpecificationPath,
                    execution.DebugServiceResolution,
                    execution.DisablePackageResolution,
                    execution.DisableHostProjectGeneration,
                    execution.UseTaskParallelisation,
                    execution.SafePackageResolution)
                    ? 0
                    : 1;
            }

            return 1;
        }

        public string GetDescription()
        {
            return @"
Synchronises the changes in the C# project files back to the
definitions and then regenerates the projects again.  If no
platform is specified, resynchronises for the current platform.
";
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

