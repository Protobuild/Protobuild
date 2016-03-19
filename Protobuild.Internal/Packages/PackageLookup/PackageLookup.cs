using System;
using Protobuild.Internal;

namespace Protobuild
{
    internal class PackageLookup : IPackageLookup
    {
        private readonly IPackageProtocol[] _packageProtocols;
        private readonly IPackageRedirector _packageRedirector;

        public PackageLookup(
            IPackageRedirector packageRedirector,
            IPackageProtocol[] packageProtocols)
        {
            _packageRedirector = packageRedirector;
            _packageProtocols = packageProtocols;
        }

        public IPackageMetadata Lookup(PackageRequestRef request)
        {
            request.Uri = _packageRedirector.RedirectPackageUrl(request.Uri);

            if (request.Uri.StartsWith("local-pointer://", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("local-pointer:// URIs should never reach this section of code.");
            }

            IPackageMetadata metadata = null;
            var schemeFound = false;
            foreach (var protocol in _packageProtocols)
            {
                foreach (var scheme in protocol.Schemes)
                {
                    if (request.Uri.StartsWith(scheme + "://"))
                    {
                        schemeFound = true;
                        metadata = protocol.ResolveSource(request);
                        break;
                    }
                }

                if (schemeFound)
                {
                    break;
                }
            }

            if (!schemeFound)
            {
                throw new InvalidOperationException("Unknown package protocol scheme for URI: " + request.Uri);
            }

            if (schemeFound && metadata == null)
            {
                throw new InvalidOperationException("Package resolution failed to complete successfully");
            }

            return metadata;
        }
    }
}