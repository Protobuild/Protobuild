using System.IO;

namespace Protobuild
{
    public class UpgradeAllPackagesCommand : ICommand
    {
        private readonly IPackageManager _packageManager;
        private readonly IHostPlatformDetector _hostPlatformDetector;

        public UpgradeAllPackagesCommand(IPackageManager packageManager, IHostPlatformDetector hostPlatformDetector)
        {
            _packageManager = packageManager;
            _hostPlatformDetector = hostPlatformDetector;
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
                    package,
                    platform,
                    null,
                    null,
                    true);
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
    }
}

