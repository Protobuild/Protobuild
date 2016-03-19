using System;

namespace Protobuild.Internal
{
    internal class GitPackageProtocol : IPackageProtocol
    {
        private readonly SourcePackageResolve _sourcePackageResolve;

        public GitPackageProtocol(SourcePackageResolve sourcePackageResolve)
        {
            _sourcePackageResolve = sourcePackageResolve;
        }

        public string[] Schemes => new[] {"local-git","http-git","https-git"};

        public IPackageMetadata ResolveSource(PackageRequestRef request)
        {
            return new GitPackageMetadata(
                NormalizeScheme(request.Uri),
                request.GitRef,
                PackageManager.PACKAGE_TYPE_LIBRARY,
                (metadata, folder, name, upgrade, source) => _sourcePackageResolve.Resolve(metadata, folder, name, upgrade));
        }

        private string NormalizeScheme(string uri)
        {
            if (uri.StartsWith("local-git://"))
            {
                return uri.Substring("local-git://".Length);
            }

            if (uri.StartsWith("http-git://"))
            {
                return "http://" + uri.Substring("http-git://".Length);
            }

            if (uri.StartsWith("https-git://"))
            {
                return "https://" + uri.Substring("https-git://".Length);
            }

            throw new InvalidOperationException("Unexpected scheme!");
        }
    }
}
