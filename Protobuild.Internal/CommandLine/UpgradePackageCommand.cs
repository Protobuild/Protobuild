using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Protobuild
{
    public class UpgradePackageCommand : ICommand
    {
        private IPackageManager _packageManager;

        public UpgradePackageCommand(IPackageManager packageManager)
        {
            _packageManager = packageManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -upgrade option");
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
                packageRef,
                "Linux",
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
            return new[] { "package_url" };
        }
    }
}

