using System;

namespace Protobuild
{
    public class QueryFeaturesCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);
        }

        public int Execute(Execution execution)
        {
            Console.WriteLine("query-features");
            return 0;
        }

        public string GetDescription()
        {
            return @"
Returns a newline-delimited list of features this version of
Protobuild supports.  This is used by Protobuild to determine
what functionality submodules support so that they can be
invoked correctly.
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
    }
}

