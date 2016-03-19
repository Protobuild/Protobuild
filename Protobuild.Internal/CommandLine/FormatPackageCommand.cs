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

        public string GetDescription()
        {
            return @"
Specifies the package format when packing a Protobuild package.  Valid
options include ""tar/gzip"" and ""tar/lzma"" (the default).  GZip format
can be uploaded via the web interface, but LZMA provides a better
compression ratio (and thus smaller package files).
";
        }

        public int GetArgCount()
        {
            return 1;
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

