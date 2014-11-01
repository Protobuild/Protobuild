using System;

namespace Protobuild
{
    public class EnableServiceCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -enable option");
            }

            pendingExecution.EnabledServices.Add(args[0]);
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Enables the specified service.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "service_name" };
        }
    }
}

