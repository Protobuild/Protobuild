using System;

namespace Protobuild
{
    internal class DisableServiceCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -disable option");
            }

            pendingExecution.DisabledServices.Add(args[0]);
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetShortCategory()
        {
            return "Service dependencies";
        }

        public string GetShortDescription()
        {
            return "disable the specified service";
        }

        public string GetDescription()
        {
            return @"
Disables the specified service.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetShortArgNames()
        {
            return new[] { "service" };
        }

        public string[] GetArgNames()
        {
            return new[] { "service_name" };
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

