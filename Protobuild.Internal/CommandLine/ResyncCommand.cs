using System;
using System.IO;

namespace Protobuild
{
    public class ResyncCommand : ICommand
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
            if (Directory.Exists("Build"))
            {
                var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                return this.m_ActionDispatch.PerformAction(
                    module,
                    "resync",
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
    }
}

