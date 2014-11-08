using System;
using System.IO;

namespace Protobuild
{
    public class HostPlatformDetector : IHostPlatformDetector
    {
        public string DetectPlatform()
        {
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

