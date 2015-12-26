using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using fastJSON;

namespace Protobuild.Internal
{
    public class LocalProtobuildPackageProtocol : IPackageProtocol
    {
        private readonly BinaryPackageResolve _binaryPackageResolve;

        public LocalProtobuildPackageProtocol(
            BinaryPackageResolve binaryPackageResolve)
        {
            _binaryPackageResolve = binaryPackageResolve;
        }

        public string[] Schemes => new[] {"local-lzma","local-gzip" };

        public IPackageMetadata ResolveSource(PackageRequestRef request)
        {
            string prefix, archiveType;
            if (request.Uri.StartsWith("local-lzma://"))
            {
                prefix = "local-lzma://";
                archiveType = PackageManager.ARCHIVE_FORMAT_TAR_LZMA;
            }
            else if (request.Uri.StartsWith("local-gzip://"))
            {
                prefix = "local-gzip://";
                archiveType = PackageManager.ARCHIVE_FORMAT_TAR_GZIP;
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
                PackageManager.PACKAGE_TYPE_LIBRARY,
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
