using System;

namespace Protobuild.Internal
{
    public class GitPackageProtocol : IPackageProtocol
    {
        public string[] Schemes => new[] {"local-git","http-git","https-git"};

        public IPackageMetadata ResolveSource(string uri, bool preferCacheLookup, string platform)
        {
            return new GitPackageMetadata
            {
                CloneURI = NormalizeScheme(uri),
                PackageType = PackageManager.PACKAGE_TYPE_LIBRARY,
            };
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
