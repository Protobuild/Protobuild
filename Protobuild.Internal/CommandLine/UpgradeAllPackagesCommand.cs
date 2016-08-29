using System.IO;

namespace Protobuild
{
    internal class UpgradeAllPackagesCommand : ICommand
    {
        private readonly IPackageManager _packageManager;
        private readonly IHostPlatformDetector _hostPlatformDetector;
        private readonly IFeatureManager _featureManager;

        public UpgradeAllPackagesCommand(IPackageManager packageManager, IHostPlatformDetector hostPlatformDetector, IFeatureManager featureManager)
        {
            _packageManager = packageManager;
            _hostPlatformDetector = hostPlatformDetector;
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
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));

            if (module.Packages == null)
            {
                return 0;
            }

            var platform = execution.Platform ?? _hostPlatformDetector.DetectPlatform();

            foreach (var package in module.Packages)
            {
                _packageManager.Resolve(
                    module,
                    package,
                    platform,
                    null,
                    null,
                    true,
                    execution.SafePackageResolution);
            }

            return 0;
        }

        public string GetDescription()
        {
            return @"
Forcibly upgrades all packages to the latest version.
WARNING: This may result in lost changes if any packages
are in source format, and you have modified files.
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
            return _featureManager.IsFeatureEnabled(Feature.PackageManagement);
        }

        public bool IsIgnored()
        {
            return false;
        }
    }
}

