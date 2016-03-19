using System;

namespace Protobuild
{
    internal class ExecuteConfigurationCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.ExecuteProjectConfiguration = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Sets the preferred configuration when executing binaries in C#
projects.  When you build in both Debug and Release modes, there
will be multiple binaries for a given C# project.  By default,
Protobuild will prefer the Debug version, but you can use this
option to explicitly set which configuration should be used.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "configuration" };
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

