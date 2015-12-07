using System;
using System.IO;

namespace Protobuild
{
    public class ResolveCommand : ICommand
    {
        private readonly IHostPlatformDetector m_HostPlatformDetector;
        private readonly IPackageManager m_PackageManager;

        public ResolveCommand(IHostPlatformDetector hostPlatformDetector, IPackageManager packageManager)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_PackageManager = packageManager;
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

            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
            var platforms = execution.Platform ?? this.m_HostPlatformDetector.DetectPlatform();
            foreach (var platform in platforms.Split(','))
            {
                this.m_PackageManager.ResolveAll(module, platform);
            }
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

