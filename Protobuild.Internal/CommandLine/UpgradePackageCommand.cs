using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Protobuild
{
    public class UpgradePackageCommand : ICommand
    {
        private readonly IPackageManager _packageManager;
        private readonly IHostPlatformDetector _hostPlatformDetector;

        public UpgradePackageCommand(IPackageManager packageManager, IHostPlatformDetector hostPlatformDetector)
        {
            _packageManager = packageManager;
            _hostPlatformDetector = hostPlatformDetector;
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
            var url = execution.PackageUrl;
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));

            if (module.Packages == null)
            {
                throw new InvalidOperationException("No such package has been added");
            }
                
            var branch = "master";
            if (url.LastIndexOf('@') > url.LastIndexOf('/'))
            {
                // A branch / commit ref is specified.
                branch = url.Substring(url.LastIndexOf('@') + 1);
                url = url.Substring(0, url.LastIndexOf('@'));
            }

            if (module.Packages.All(x => x.Uri != url))
            {
                throw new InvalidOperationException("No such package has been added");
            }

            var packageRef = module.Packages.First(x => x.Uri == url);

            _packageManager.Resolve(
                module,
                packageRef,
                execution.Platform ?? _hostPlatformDetector.DetectPlatform(),
                null,
                null,
                true);

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
    }
}

