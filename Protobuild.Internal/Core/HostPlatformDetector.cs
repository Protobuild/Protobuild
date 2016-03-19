using System;
using System.IO;

namespace Protobuild
{
    internal class HostPlatformDetector : IHostPlatformDetector
    {
        public static string SimulatedHostPlatform { get; set; }

        public string DetectPlatform()
        {
            if (SimulatedHostPlatform != null)
            {
                return SimulatedHostPlatform;
            }

            if (Path.DirectorySeparatorChar == '/')
            {
                if (Directory.Exists("/Library"))
                {
                    return "MacOS";
                }

                return "Linux";
            }

            return "Windows";
        }
    }
}

