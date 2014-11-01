using System;
using System.IO;

namespace Protobuild
{
    public class AddPackageCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -add option");
            }

            pendingExecution.PackageName = args[0];
        }

        public int Execute(Execution execution)
        {
            Actions.AddSubmodule(ModuleInfo.Load(Path.Combine("Build", "Module.xml")), execution.PackageName);

            return 0;
        }

        public string GetDescription()
        {
            return @"
Add a package to the current module.
";
        }

        public int GetArgCount()
        {
            return 1;
        }
    }
}

