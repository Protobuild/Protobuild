using System;

namespace Protobuild.Internal
{
    internal class NuGetPackageProtocol : IPackageProtocol
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
            return new TransformedPackageMetadata(
                NormalizeScheme(request.Uri),
                PackageManager.PACKAGE_TYPE_LIBRARY,
                request.Platform,
                request.GitRef,
                _transformer,
                (metadata, folder, name, upgrade, preferSource) =>
                {
                    _binaryPackageResolve.Resolve(metadata, folder, name, upgrade);
                },
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
