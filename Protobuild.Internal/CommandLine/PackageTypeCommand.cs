using System;
using System.IO;

namespace Protobuild
{
    internal class PackageTypeCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public PackageTypeCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the package type to -package-type.");
            }

            switch (args[0])
            {
                case PackageManager.PACKAGE_TYPE_LIBRARY:
                case PackageManager.PACKAGE_TYPE_GLOBAL_TOOL:
                case PackageManager.PACKAGE_TYPE_TEMPLATE:
                    pendingExecution.PackageType = args[0];
                    break;
                default:
                    throw new InvalidOperationException("The package type must be one of '" + PackageManager.PACKAGE_TYPE_LIBRARY + "', '" + PackageManager.PACKAGE_TYPE_GLOBAL_TOOL + "' or '" + PackageManager.PACKAGE_TYPE_TEMPLATE + "'.");
            }
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Sets the package type.  By default, Protobuild creates library packages,
but you can use this option to create '" + PackageManager.PACKAGE_TYPE_GLOBAL_TOOL + @"' 
or '" + PackageManager.PACKAGE_TYPE_TEMPLATE + @"' packages instead.  This option has no effect
for packages not stored in the nuget/zip format.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "package-type" };
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

