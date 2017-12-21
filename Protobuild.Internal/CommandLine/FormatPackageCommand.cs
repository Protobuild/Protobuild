using System;
using System.IO;

namespace Protobuild
{
    internal class FormatPackageCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public FormatPackageCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the package format to -format.");
            }
                
            switch (args[0])
            {
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                case PackageManager.ARCHIVE_FORMAT_NUGET_ZIP:
                    pendingExecution.PackageFormat = args[0];
                    break;
                default:
                    throw new InvalidOperationException("Unknown package format " + args[0]);
            }
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetShortCategory()
        {
            // We want people to just use NuGet by default - we will be removing Protobuild
            // package types in the future, at which point this option will become a no-op)
            return "Internal use";
        }

        public string GetShortDescription()
        {
            return "set the package format to use for packaging (default nuget/zip)";
        }

        public string GetDescription()
        {
            return @"
Specifies the package format when creating a Protobuild package.  Valid
options include ""nuget/zip"" (the default).
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetShortArgNames()
        {
            return GetArgNames();
        }

        public string[] GetArgNames()
        {
            return new[] { "format" };
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

