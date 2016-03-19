using System;

namespace Protobuild
{
    internal class BuildTargetCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1)
            {
                throw new InvalidOperationException(
                    "You must provide the name of the build target if you use --build-target.");
            }

            pendingExecution.BuildTarget = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Specifies the build target to use for MSBuild / xbuild.  Defaults
to ""Build"".";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "build_target" };
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

