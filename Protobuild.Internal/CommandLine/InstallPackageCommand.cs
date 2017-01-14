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

        private readonly IPackageUrlParser _packageUrlParser;

        public InstallPackageCommand(
            IHostPlatformDetector hostPlatformDetector,
            IPackageManager packageManager,
            IFeatureManager featureManager,
            IPackageUrlParser packageUrlParser)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_PackageManager = packageManager;
            _featureManager = featureManager;
            _packageUrlParser = packageUrlParser;
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
            var package = _packageUrlParser.Parse(execution.PackageUrl);

            RedirectableConsole.WriteLine("Installing " + package.Uri + "...");
            this.m_PackageManager.Resolve(null, package, this.m_HostPlatformDetector.DetectPlatform(), null, false, true, false);

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

