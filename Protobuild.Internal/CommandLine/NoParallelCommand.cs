using System;

namespace Protobuild
{
    internal class NoParallelCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.UseTaskParallelisation = false;
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetShortCategory()
        {
            return "Internal use";
        }

        public string GetShortDescription()
        {
            return "disable parallelism";
        }

        public string GetDescription()
        {
            return @"
Disables task parallelisation internally.
";
        }

        public int GetArgCount()
        {
            return 0;
        }

        public string[] GetShortArgNames()
        {
            return GetArgNames();
        }

        public string[] GetArgNames()
        {
            return new string[0];
        }

        public bool IsInternal()
        {
            return true;
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
