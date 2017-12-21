using System;
using System.IO;

namespace Protobuild
{
    internal class NoHostGenerateCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.DisableHostProjectGeneration = true;
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
            return "prevent generate of host platform projects during project generation";
        }

        public string GetDescription()
        {
            return @"
Prevents generation of host platform projects during --generate.
This assumes that you have previously generated the projects for
the host platform.
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

