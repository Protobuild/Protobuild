using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Protobuild
{
    internal class RemovePackageCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        private readonly IPackageUrlParser _packageUrlParser;

        public RemovePackageCommand(IPackageUrlParser packageUrlParser, IFeatureManager featureManager)
        {
            _packageUrlParser = packageUrlParser;
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -remove option");
            }

            pendingExecution.PackageUrl = args[0];
        }

        public int Execute(Execution execution)
        {
            var url = execution.PackageUrl;
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));

            if (module.Packages == null)
            {
                module.Packages = new List<PackageRef>();
            }

            var packageRef = _packageUrlParser.Parse(url);

            foreach (var package in module.Packages.ToArray())
            {
                if (package.Uri == packageRef.Uri)
                {
                    RedirectableConsole.WriteLine("Removing " + package.Uri + "...");
                    module.Packages.Remove(package);

                    // Save after each package remove in case something goes wrong deleting
                    // the directory.
                    module.Save(Path.Combine("Build", "Module.xml"));

                    if (Directory.Exists(Path.Combine(module.Path, package.Folder)))
                    {
                        RedirectableConsole.WriteLine("Deleting folder '" + package.Folder + "'...");
                        PathUtils.AggressiveDirectoryDelete(Path.Combine(module.Path, package.Folder));
                    }
                }
            }

            return 0;
        }

        public string GetDescription()
        {
            return @"
Remove a package and delete the associated folder from the current module.
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

