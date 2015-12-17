using System;
using System.IO;

namespace Protobuild
{
    public class NoResolveCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public NoResolveCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.DisablePackageResolution = true;
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Prevents package resolution occurring when any of the standard
actions are used.  This option is used when Protobuild calls
into submodules, as package resolution has already occurred
for submodules during the main package resolution step.
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
            return true;
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

