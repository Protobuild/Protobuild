using System;
using System.IO;

namespace Protobuild
{
    internal class AutomatedBuildCommand : ICommand
    {
        private readonly IAutomatedBuildController _automatedBuildController;

        public AutomatedBuildCommand(IAutomatedBuildController automatedBuildController)
        {
            _automatedBuildController = automatedBuildController;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length > 0)
            {
                pendingExecution.AutomatedBuildScriptPath = args[0];
            }
        }

        public int Execute(Execution execution)
        {
            var scriptPath = execution.AutomatedBuildScriptPath ?? "automated.build";

            if (!File.Exists(scriptPath))
            {
                Console.Error.WriteLine("ERROR: Automated build script not found at " + scriptPath + ".");
            }

            return _automatedBuildController.Execute(scriptPath);
        }

        public string GetDescription()
        {
            return @"
Executes the automated build script located at the specified
path (automated.build by default).  This allows you to combine
multiple Protobuild commands together for continuous integration
(build) servers.";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] {"script_path?"};
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