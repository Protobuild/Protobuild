using System;

namespace Protobuild.Internal
{
    public class NuGetPackageProtocol : IPackageProtocol
    {
        private readonly NuGetPackageTransformer _transformer;
        private readonly BinaryPackageResolve _binaryPackageResolve;

        public NuGetPackageProtocol(NuGetPackageTransformer transformer, BinaryPackageResolve binaryPackageResolve)
        {
            _transformer = transformer;
            _binaryPackageResolve = binaryPackageResolve;
        }

        public string[] Schemes => new[] {"http-nuget", "https-nuget" };

        public IPackageMetadata ResolveSource(PackageRequestRef request)
        {
            return new NuGetPackageMetadata(
                NormalizeScheme(request.Uri),
                PackageManager.PACKAGE_TYPE_LIBRARY,
                (metadata, folder, name, upgrade, preferSource) =>
                {
                    var protobuild = ConvertToProtobuildMetadata((NuGetPackageMetadata)metadata, request);
                    protobuild.Resolve(metadata, folder, name, upgrade, false);
                },
                (IPackageMetadata metadata, out string archiveType, out byte[] packageData) =>
                {
                    var protobuild = ConvertToProtobuildMetadata((NuGetPackageMetadata)metadata, request);
                    protobuild.GetProtobuildPackageBinary(metadata, out archiveType, out packageData);
                });
        }

        private ProtobuildPackageMetadata ConvertToProtobuildMetadata(NuGetPackageMetadata metadata, PackageRequestRef request)
        {
            return new ProtobuildPackageMetadata(
                request.Uri,
                metadata.PackageType,
                null,
                request.Platform,
                request.GitRef,
                PackageManager.ARCHIVE_FORMAT_TAR_LZMA,
                null,
                _transformer,
                (metadata2, folder, name, upgrade, source) => _binaryPackageResolve.Resolve(metadata2, folder, name, upgrade),
                _binaryPackageResolve.GetProtobuildPackageBinary);
        }

        private string NormalizeScheme(string uri)
        {
            if (uri.StartsWith("http-nuget://"))
            {
                return "http://" + uri.Substring("http-nuget://".Length);
            }

            if (uri.StartsWith("https-nuget://"))
            {
                return "https://" + uri.Substring("https-nuget://".Length);
            }

            throw new InvalidOperationException("Unexpected scheme!");
        }
    }
}
