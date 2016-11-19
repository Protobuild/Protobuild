using System.Collections.Generic;

namespace Protobuild
{
    internal class AutomaticModulePackager : IAutomaticModulePackager
    {
        private readonly ProtobuildAutomaticModulePackager _protobuildAutomaticModulePackager;
        private readonly NuGetAutomaticModulePackager _nuGetAutomaticModulePackager;

        public AutomaticModulePackager(
            ProtobuildAutomaticModulePackager protobuildAutomaticModulePackager,
            NuGetAutomaticModulePackager nuGetAutomaticModulePackager)
        {
            _protobuildAutomaticModulePackager = protobuildAutomaticModulePackager;
            _nuGetAutomaticModulePackager = nuGetAutomaticModulePackager;
        }

        public void Autopackage(FileFilter fileFilter, Execution execution, ModuleInfo module, string rootPath, string platform, string packageFormat, List<string> temporaryFiles)
        {
            switch (packageFormat)
            {
                case PackageManager.ARCHIVE_FORMAT_NUGET_ZIP:
                    _nuGetAutomaticModulePackager.Autopackage(
                        fileFilter,
                        execution,
                        module,
                        rootPath,
                        platform,
                        packageFormat,
                        temporaryFiles);
                    break;
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                default:
                    _protobuildAutomaticModulePackager.Autopackage(
                        fileFilter,
                        execution,
                        module,
                        rootPath,
                        platform,
                        packageFormat,
                        temporaryFiles);
                    break;
            }
        }
    }
}

