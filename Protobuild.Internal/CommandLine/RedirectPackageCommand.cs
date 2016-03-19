using System;

namespace Protobuild
{
    internal class RedirectPackageCommand : ICommand
    {
        private readonly IPackageRedirector m_PackageRedirector;

        private readonly IFeatureManager _featureManager;

        public RedirectPackageCommand(IPackageRedirector packageRedirector, IFeatureManager featureManager)
        {
            this.m_PackageRedirector = packageRedirector;
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 2 || args[0] == null || args[1] == null)
            {
                throw new InvalidOperationException("You must provide both the original and target URLs to the -redirect option");
            }

            this.m_PackageRedirector.RegisterLocalRedirect(args[0], args[1]);
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Redirects any packages using the original URL to the target URL.
";
        }

        public int GetArgCount()
        {
            return 2;
        }

        public string[] GetArgNames()
        {
            return new[] { "original_url", "target_url" };
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

