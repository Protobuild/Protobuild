using System;

namespace Protobuild
{
    internal class FeaturesCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public FeaturesCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length > 0)
            {
                _featureManager.LoadFeaturesFromCommandLine(args[0]);
            }
            else
            {
                _featureManager.LoadFeaturesFromCommandLine(string.Empty);
            }
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription ()
        {
            return @"
Used internally to pass the feature set from a parent module to
a submodule.
";
        }

        public int GetArgCount ()
        {
            return 1;
        }

        public string[] GetArgNames ()
        {
            return new[] { "feature_list" };
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

