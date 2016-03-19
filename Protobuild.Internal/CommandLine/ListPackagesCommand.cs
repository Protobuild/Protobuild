using System;
using System.IO;
using System.Collections.Generic;

namespace Protobuild
{
    internal class ListPackagesCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public ListPackagesCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);
        }

        public int Execute(Execution execution)
        {
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));

            if (module.Packages == null)
            {
                module.Packages = new List<PackageRef>();
            }

            foreach (var package in module.Packages)
            {
                Console.WriteLine(package.Uri);
            }

            return 0;
        }

        public string GetDescription()
        {
            return @"
Lists the URLs of all packages in this module.
";
        }

        public int GetArgCount()
        {
            return 0;
        }

        public string[] GetArgNames()
        {
            return new string[0];
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

