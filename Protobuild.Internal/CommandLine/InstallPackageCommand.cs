using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Protobuild
{
    internal class InstallPackageCommand : ICommand
    {
        private readonly IHostPlatformDetector m_HostPlatformDetector;
        private readonly IPackageManager m_PackageManager;

        private readonly IFeatureManager _featureManager;

        public InstallPackageCommand(
            IHostPlatformDetector hostPlatformDetector,
            IPackageManager packageManager,
            IFeatureManager featureManager)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_PackageManager = packageManager;
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -install option");
            }

            pendingExecution.PackageUrl = args[0];
        }

        public int Execute(Execution execution)
        {
            var url = execution.PackageUrl;

            var branch = "master";
            if (url.LastIndexOf('@') > url.LastIndexOf('/'))
            {
                // A branch / commit ref is specified.
                branch = url.Substring(url.LastIndexOf('@') + 1);
                url = url.Substring(0, url.LastIndexOf('@'));
            }

            var package = new PackageRef
            {
                Uri = url,
                GitRef = branch,
                Folder = null
            };

            Console.WriteLine("Installing " + url + "...");
            this.m_PackageManager.Resolve(null, package, this.m_HostPlatformDetector.DetectPlatform(), null, false, true);

            return 0;
        }

        public string GetDescription()
        {
            return @"
Installs a global tool package without adding it as a dependency
of the current module.  If the current module should depend on
this global tool, use --add instead.
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

