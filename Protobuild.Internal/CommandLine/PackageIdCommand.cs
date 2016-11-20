using System;
using System.IO;

namespace Protobuild
{
    internal class PackageIdCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public PackageIdCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the package ID to -package-id.");
            }

            pendingExecution.PackageId = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Overrides the package ID embedded into the package.  If this option is
not used, Protobuild will use the module name.  This option has no effect
for packages not stored in the nuget/zip format.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "package-id" };
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

