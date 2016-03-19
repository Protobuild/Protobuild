using System;

namespace Protobuild
{
    internal class PackageRootCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public PackageRootCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -package-root option");
            }

            if (pendingExecution.ServiceSpecificationPath != null)
            {
                throw new InvalidOperationException("Multiple -package-root options passed.");
            }

            pendingExecution.PackageRoot = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Internally used to pass the package root.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "path" };
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

