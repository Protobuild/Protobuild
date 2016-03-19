using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using fastJSON;

namespace Protobuild.Internal
{
    internal class LocalProtobuildPackageProtocol : IPackageProtocol
    {
        private readonly BinaryPackageResolve _binaryPackageResolve;

        public LocalProtobuildPackageProtocol(
            BinaryPackageResolve binaryPackageResolve)
        {
            _binaryPackageResolve = binaryPackageResolve;
        }

        public string[] Schemes => new[] {"local-lzma","local-gzip","local-tool-lzma","local-tool-gzip" };

        public IPackageMetadata ResolveSource(PackageRequestRef request)
        {
            string prefix, archiveType, packageType;
            if (request.Uri.StartsWith("local-lzma://"))
            {
                prefix = "local-lzma://";
                archiveType = PackageManager.ARCHIVE_FORMAT_TAR_LZMA;
                packageType = PackageManager.PACKAGE_TYPE_LIBRARY;
            }
            else if (request.Uri.StartsWith("local-gzip://"))
            {
                prefix = "local-gzip://";
                archiveType = PackageManager.ARCHIVE_FORMAT_TAR_GZIP;
                packageType = PackageManager.PACKAGE_TYPE_LIBRARY;
            }
            else if (request.Uri.StartsWith("local-tool-lzma://"))
            {
                prefix = "local-tool-lzma://";
                archiveType = PackageManager.ARCHIVE_FORMAT_TAR_LZMA;
                packageType = PackageManager.PACKAGE_TYPE_GLOBAL_TOOL;
            }
            else if (request.Uri.StartsWith("local-tool-gzip://"))
            {
                prefix = "local-tool-gzip://";
                archiveType = PackageManager.ARCHIVE_FORMAT_TAR_GZIP;
                packageType = PackageManager.PACKAGE_TYPE_GLOBAL_TOOL;
            }
            else
            {
                throw new InvalidOperationException("Unexpected prefix for local package URL " + request.Uri);
            }

            var localPackagePath = request.Uri.Substring(prefix.Length);

            if (!File.Exists(localPackagePath))
            {
                throw new InvalidOperationException("Unable to find local package file " + localPackagePath + " on disk.");
            }

            return new ProtobuildPackageMetadata(
                null,
                packageType,
                null,
                request.Platform,
                request.GitRef,
                archiveType,
                localPackagePath,
                (metadata, folder, name, upgrade, source) =>
                {
                    _binaryPackageResolve.Resolve(metadata, folder, name, upgrade);
                },
                _binaryPackageResolve.GetProtobuildPackageBinary);
        }
    }
}
