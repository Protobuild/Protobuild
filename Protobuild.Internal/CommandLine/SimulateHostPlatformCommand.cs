using System;
using System.IO;

namespace Protobuild
{
    internal class SimulateHostPlatformCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the name of the host platform to simulate");
            }

            switch (args[0])
            {
                case "Windows":
                case "MacOS":
                case "Linux":
                    break;
                default:
                    throw new InvalidOperationException("Invalid host platform name for simulation");
                    break;
            }

            HostPlatformDetector.SimulatedHostPlatform = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Forces a different host platform name.  This is only used for
functionally testing that Protobuild's behaviour is correct
under different platforms.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "platform" };
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

