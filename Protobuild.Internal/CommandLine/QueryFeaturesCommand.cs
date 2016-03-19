using System;

namespace Protobuild
{
    internal class QueryFeaturesCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public QueryFeaturesCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);
        }

        public int Execute(Execution execution)
        {
            foreach (var featureId in _featureManager.GetEnabledInternalFeatureIDs())
            {
                Console.WriteLine(featureId);
            }
            return 0;
        }

        public string GetDescription()
        {
            return @"
Returns a newline-delimited list of features this version of
Protobuild supports.  This is used by Protobuild to determine
what functionality submodules support so that they can be
invoked correctly.
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
            return false;
        }
    }
}

