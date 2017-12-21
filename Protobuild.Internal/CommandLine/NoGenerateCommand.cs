using System;
using System.IO;

namespace Protobuild
{
    internal class NoGenerateCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.DisableProjectGeneration = true;
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetShortCategory()
        {
            return "Customisation";
        }

        public string GetShortDescription()
        {
            return "prevent project generation after using --start";
        }

        public string GetDescription()
        {
            return @"
Prevents project generation occurring after --start is used.
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

