using System;

namespace Protobuild.Internal
{
    public class NuGetPackageProtocol : IPackageProtocol
    {
        public string[] Schemes => new[] {"http-nuget", "https-nuget" };

        public IPackageMetadata ResolveSource(string uri, bool preferCacheLookup, string platform)
        {
            return new NuGetPackageMetadata
            {
                SourceURI = NormalizeScheme(uri),
                PackageType = PackageManager.PACKAGE_TYPE_LIBRARY,
            };
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
