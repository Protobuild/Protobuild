using System;
using System.IO;

namespace Protobuild
{
    internal class ResolveCommand : ICommand
    {
        private readonly IHostPlatformDetector m_HostPlatformDetector;
        private readonly IPackageManager m_PackageManager;
        private readonly IFeatureManager _featureManager;

        public ResolveCommand(IHostPlatformDetector hostPlatformDetector, IPackageManager packageManager, IFeatureManager featureManager)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_PackageManager = packageManager;
            _featureManager = featureManager;
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
                this.m_PackageManager.ResolveAll(module, platform, execution.UseTaskParallelisation, false, execution.SafePackageResolution);
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
            return !_featureManager.IsFeatureEnabled(Feature.PackageManagement);
        }
    }
}

