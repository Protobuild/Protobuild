using System;
using System.IO;

namespace Protobuild
{
    internal class NoResolveCommand : ICommand
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

        public string GetShortCategory()
        {
            return "Customisation";
        }

        public string GetShortDescription()
        {
            return "disable package resolution during project generation";
        }


        public string GetDescription()
        {
            return @"
Prevents package resolution occurring when any of the standard
actions are used.
";
        }

        public int GetArgCount()
        {
            return 0;
        }

        public string[] GetShortArgNames()
        {
            return GetArgNames();
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

