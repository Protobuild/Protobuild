using System;
using System.IO;

namespace Protobuild
{
    public class GenerateCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            Console.WriteLine("Encountered generate command");

            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            Console.WriteLine(args[0]);

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
                return Actions.PerformAction(
                    module,
                    "generate",
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
Generates the C# project files from the definitions.  If no
platform is specified, generates for the current platform.
";
        }

        public int GetArgCount()
        {
            return 1;
        }
    }
}

