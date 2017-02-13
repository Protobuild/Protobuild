using System;

namespace Protobuild
{
    internal class DebugProjectGenerationCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.DebugProjectGeneration = true;
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Turns on debugging during project generation, emitting the input files
used for the XSLT generation.
";
        }

        public int GetArgCount()
        {
            return 0;
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

