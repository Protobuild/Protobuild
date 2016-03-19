using System;

namespace Protobuild
{
    internal class IgnoreOnExistingPackageCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public IgnoreOnExistingPackageCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.PackagePushIgnoreOnExisting = true;
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Indicates that Protobuild should return with a successful exit code if
there is already a package pushed with this version hash and platform,
rather than exiting with an error code (the default).  This is useful
in automated build scripts, where the push for one platform may be re-run.
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
            return true;
        }

        public bool IsIgnored()
        {
            return !_featureManager.IsFeatureEnabled(Feature.PackageManagement);
        }
    }
}

