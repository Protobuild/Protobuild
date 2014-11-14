using System;
using System.IO;

namespace Protobuild
{
    public class SwapToSourceCommand : ICommand
    {
        private readonly IHostPlatformDetector m_HostPlatformDetector;

        public SwapToSourceCommand(IHostPlatformDetector hostPlatformDetector)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the URL of the package to swap to source.");
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
                    Console.WriteLine("Switching to source: " + submodule.Uri);
                    submoduleManager.Resolve(submodule, platform, null, true);
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
Swaps the specified package into it's source version (if possible).
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

