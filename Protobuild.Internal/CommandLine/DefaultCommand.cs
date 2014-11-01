using System;
using System.IO;

namespace Protobuild
{
    public class DefaultCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            throw new NotSupportedException();
        }

        public int Execute(Execution execution)
        {
            if (!File.Exists(Path.Combine("Build", "Module.xml")))
            {
                Directory.CreateDirectory("Build");
                ResourceExtractor.ExtractAll(Path.Combine(Environment.CurrentDirectory, "Build"), "MyProject");
                Console.WriteLine("Build" + Path.DirectorySeparatorChar + "Module.xml has been created.");
                Environment.Exit(0);
            }

            return Actions.DefaultAction(
                ModuleInfo.Load(Path.Combine("Build", "Module.xml")),
                enabledServices: execution.EnabledServices.ToArray(),
                disabledServices: execution.DisabledServices.ToArray(),
                serviceSpecPath: execution.ServiceSpecificationPath) ? 0 : 1;
        }

        public string GetDescription()
        {
            throw new NotSupportedException();
        }

        public int GetArgCount()
        {
            throw new NotSupportedException();
        }
    }
}

