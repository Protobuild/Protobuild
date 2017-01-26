using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Protobuild
{
    internal class UpgradePackageCommand : ICommand
    {
        private readonly IPackageManager _packageManager;
        private readonly IHostPlatformDetector _hostPlatformDetector;
        private readonly IPackageNameLookup _packageNameLookup;
        private readonly IFeatureManager _featureManager;
        private readonly IPackageUrlParser _packageUrlParser;

        public UpgradePackageCommand(
            IPackageManager packageManager,
            IHostPlatformDetector hostPlatformDetector,
            IPackageNameLookup packageNameLookup,
            IFeatureManager featureManager,
            IPackageUrlParser packageUrlParser)
        {
            _packageManager = packageManager;
            _hostPlatformDetector = hostPlatformDetector;
            _packageNameLookup = packageNameLookup;
            _featureManager = featureManager;
            _packageUrlParser = packageUrlParser;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -upgrade option");
            }

            if (args.Length > 1)
            {
                pendingExecution.Platform = args[1];
            }

            pendingExecution.PackageUrl = args[0];
        }

        public int Execute(Execution execution)
        {
            var module = ModuleInfo.Load(Path.Combine(execution.WorkingDirectory, "Build", "Module.xml"));

            if (module.Packages == null)
            {
                throw new InvalidOperationException("No such package has been added");
            }

            var package = _packageUrlParser.Parse(execution.PackageUrl);

            var packageRef = _packageNameLookup.LookupPackageByName(module, package.Uri);

            _packageManager.Resolve(
                execution.WorkingDirectory,
                module,
                packageRef,
                execution.Platform ?? _hostPlatformDetector.DetectPlatform(),
                null,
                null,
                true,
                execution.SafePackageResolution);

            return 0;
        }

        public string GetDescription()
        {
            return @"
Forcibly upgrades the package to the latest version.
WARNING: This may result in lost changes if the package
is in source format, and you have modified files.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "package_url", "platform?" };
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

