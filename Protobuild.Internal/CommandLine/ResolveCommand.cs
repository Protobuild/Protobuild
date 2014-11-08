using System;
using System.IO;

namespace Protobuild
{
    public class ResolveCommand : ICommand
    {
        private readonly IHostPlatformDetector m_HostPlatformDetector;

        public ResolveCommand(IHostPlatformDetector hostPlatformDetector)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length > 0)
            {
                pendingExecution.Platform = args[0];
            }
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
            submoduleManager.ResolveAll(module, platform);
            return 0;
        }

        public string GetDescription()
        {
            return @"
Resolves packages for the current module.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "platform?" };
        }
    }
}

