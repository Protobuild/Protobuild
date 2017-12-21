using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Protobuild
{
    internal class AddPackageCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        private readonly IPackageUrlParser _packageUrlParser;

        public AddPackageCommand(IPackageUrlParser packageUrlParser, IFeatureManager featureManager)
        {
            _packageUrlParser = packageUrlParser;
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -add option");
            }

            pendingExecution.PackageUrl = args[0];
        }

        public int Execute(Execution execution)
        {
            var url = execution.PackageUrl;
            var module = ModuleInfo.Load(Path.Combine(execution.WorkingDirectory, "Build", "Module.xml"));

            if (module.Packages == null)
            {
                module.Packages = new List<PackageRef>();
            }

            var package = _packageUrlParser.Parse(url);

            if (module.Packages.Any(x => x.Uri == package.Uri))
            {
                RedirectableConsole.WriteLine("WARNING: Package with URI " + package.Uri + " is already present; ignoring request to add package.");
                return 0;
            }

            if (Directory.Exists(Path.Combine(module.Path, package.Folder)))
            {
                throw new InvalidOperationException(package.Folder + " already exists");
            }

            RedirectableConsole.WriteLine("Adding " + package.Uri + " as " + package.Folder + "...");
            module.Packages.Add(package);
            module.Save(Path.Combine(execution.WorkingDirectory, "Build", "Module.xml"));

            return 0;
        }

        public string GetShortCategory()
        {
            return "Package management";
        }

        public string GetShortDescription()
        {
            return "add a package to the module without downloading or installing it";
        }

        public string GetDescription()
        {
            return @"
Add a package to the current module.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetShortArgNames()
        {
            return new[] { "package" };
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

