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
            Console.WriteLine("no-resolve");
            Console.WriteLine("list-packages");
            Console.WriteLine("skip-invocation-on-no-standard-projects");
            Console.WriteLine("skip-synchronisation-on-no-standard-projects");
            Console.WriteLine("skip-resolution-on-no-packages-or-submodules");
            Console.WriteLine("inline-invocation-if-identical-hashed-executables");
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

