using System;

namespace Protobuild
{
    internal class BuildPropertyCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1)
            {
                throw new InvalidOperationException(
                    "You must provide the name of the build property if you use --build-property.");
            }

            pendingExecution.BuildProperties.Add(args[0], args.Length >= 2 ? args[1] : null);
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Add or set a build property to use during the build.  If you omit
the property value, it is assumed to be an empty string.";
        }

        public int GetArgCount()
        {
            return 2;
        }

        public string[] GetArgNames()
        {
            return new[] { "name", "value?" };
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

