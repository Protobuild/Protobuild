using System;

namespace Protobuild
{
    public class ServiceSpecificationCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -spec option");
            }

            if (pendingExecution.ServiceSpecificationPath != null)
            {
                throw new InvalidOperationException("Multiple -spec options passed.");
            }

            pendingExecution.ServiceSpecificationPath = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Internally used to pass the service specification.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "spec_path" };
        }
    }
}

