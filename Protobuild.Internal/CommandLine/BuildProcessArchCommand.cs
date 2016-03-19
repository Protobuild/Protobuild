using System;

namespace Protobuild
{
    internal class BuildProcessArchCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1)
            {
                throw new InvalidOperationException(
                    "You must provide the name of the build process architecture if you use the --build-process-arch property.");
            }

            pendingExecution.BuildProcessArchitecture = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Sets the build process's architecture.  On Windows, MSBuild ships in
both 32-bit and 64-bit formats, but some specialised build tasks
require that MSBuild be running on a specific architecture (to match
that of native code).  By default Protobuild uses an architecture
suitable for building the target platform, but you can override the
build process's architecture by passing 'x86' (32-bit) or 'x64'
(64-bit) to this property.  If you pass 'Default' to this property,
Protobuild will use the default behaviour.";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "arch" };
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

