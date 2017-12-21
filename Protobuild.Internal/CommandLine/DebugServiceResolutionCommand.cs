using System;

namespace Protobuild
{
    internal class DebugServiceResolutionCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.DebugServiceResolution = true;
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
            return "enable verbose output for service resolution";
        }

        public string GetDescription()
        {
            return @"
Turns on debugging during service resolution, showing each action
taken during each pass of service resolution.
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

