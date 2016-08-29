using System;
using System.IO;

namespace Protobuild
{
    internal class SafeResolveCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public SafeResolveCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SafePackageResolution = true;
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Prevents package resolution from aggressively removing content
from the directories where packages will be resolved to.  You
only need to enable this option if you have package directories
which are written to by another process (e.g. git submodules).
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

