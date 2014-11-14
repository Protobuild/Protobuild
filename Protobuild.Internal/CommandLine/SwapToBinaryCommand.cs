using System;
using System.IO;

namespace Protobuild
{
    public class SwapToBinaryCommand : ICommand
    {
        private readonly IHostPlatformDetector m_HostPlatformDetector;

        public SwapToBinaryCommand(IHostPlatformDetector hostPlatformDetector)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the URL of the package to swap to binary.");
            }

            pendingExecution.PackageUrl = args[0];
        }

        public int Execute(Execution execution)
        {
            if (!File.Exists(Path.Combine("Build", "Module.xml")))
            {
                throw new InvalidOperationException("No module present.");
            }

            var platform = execution.Platform ?? this.m_HostPlatformDetector.DetectPlatform();
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
            var submoduleManager = new PackageManager();

            var done = false;
            foreach (var submodule in module.Packages)
            {
                if (submodule.Uri == execution.PackageUrl)
                {
                    Console.WriteLine("Switching to binary: " + submodule.Uri);
                    submoduleManager.Resolve(submodule, platform, null, false);
                    done = true;
                    break;
                }
            }

            if (!done)
            {
                Console.WriteLine("No package registered with URL " + execution.PackageUrl);
                return 1;
            }

            return 0;
        }

        public string GetDescription()
        {
            return @"
Swaps the specified package into it's binary version (if possible).
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "package_url" };
        }
    }
}

