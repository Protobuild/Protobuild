using System;
using System.IO;

namespace Protobuild
{
    public class SyncCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;

        public SyncCommand(IActionDispatch actionDispatch)
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
            if (Directory.Exists("Build"))
            {
                var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                return this.m_ActionDispatch.PerformAction(
                    module,
                    "sync",
                    execution.Platform,
                    execution.EnabledServices.ToArray(),
                    execution.DisabledServices.ToArray(),
                    execution.ServiceSpecificationPath)
                    ? 0
                    : 1;
            }

            return 1;
        }

        public string GetDescription()
        {
            return @"
Synchronises the changes in the C# project files back to the
definitions, but does not regenerate the projects.  If no
platform is specified, synchronises for the current platform.
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
    }
}

