using System;
using System.Net;
using fastJSON;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Protobuild.Internal;

namespace Protobuild
{
    public class PackageLookup : IPackageLookup
    {
        private IPackageRedirector _packageRedirector;
        private NuGetPackageTransformer _nugetPackageTransformer;
        private readonly IPackageProtocol[] _packageProtocols;

        public PackageLookup(
            IPackageRedirector packageRedirector,
            NuGetPackageTransformer nugetPackageTransformer,
            IPackageProtocol[] packageProtocols)
        {
            _packageRedirector = packageRedirector;
            _nugetPackageTransformer = nugetPackageTransformer;
            _packageProtocols = packageProtocols;
        }

        public void Lookup(
            string uri,
            string platform,
            bool preferCacheLookup,
            out string sourceUri,
            out string sourceFormat,
            out string type,
            out Dictionary<string, string> downloadMap,
            out Dictionary<string, string> archiveTypeMap,
            out Dictionary<string, string> resolvedHash,
            out IPackageTransformer transformer)
        {
            uri = _packageRedirector.RedirectPackageUrl(uri);
            transformer = null;

            if (uri.StartsWith("local-pointer://", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("local-pointer:// URIs should never reach this section of code.");
            }

            IPackageMetadata metadata = null;
            foreach (var protocol in _packageProtocols)
            {
                foreach (var scheme in protocol.Schemes)
                {
                    if (uri.StartsWith(scheme + "://"))
                    {
                        metadata = protocol.ResolveSource(uri, preferCacheLookup, platform);
                        break;
                    }
                }

                if (metadata != null)
                {
                    break;
                }
            }

            var gitMetadata = metadata as GitPackageMetadata;
            var folderMetadata = metadata as FolderPackageMetadata;
            var nuGetMetadata = metadata as NuGetPackageMetadata;
            var protobuildMetadata = metadata as ProtobuildPackageMetadata;

            if (folderMetadata != null)
            {
                sourceUri = folderMetadata.Folder;
                sourceFormat = PackageManager.SOURCE_FORMAT_DIRECTORY;
                type = folderMetadata.PackageType;
                downloadMap = new Dictionary<string, string>();
                archiveTypeMap = new Dictionary<string, string>();
                resolvedHash = new Dictionary<string, string>();
                return;
            }

            if (gitMetadata != null)
            {
                sourceUri = gitMetadata.CloneURI;
                sourceFormat = PackageManager.SOURCE_FORMAT_GIT;
                type = gitMetadata.PackageType;
                downloadMap = new Dictionary<string, string>();
                archiveTypeMap = new Dictionary<string, string>();
                resolvedHash = new Dictionary<string, string>();
                return;
            }

            if (nuGetMetadata != null)
            {
                sourceUri = nuGetMetadata.SourceURI;
                sourceFormat = PackageManager.SOURCE_FORMAT_GIT;
                type = nuGetMetadata.PackageType;
                downloadMap = new Dictionary<string, string>();
                archiveTypeMap = new Dictionary<string, string>();
                resolvedHash = new Dictionary<string, string>();
                transformer = _nugetPackageTransformer;
                return;
            }

            if (protobuildMetadata != null)
            {
                sourceUri = protobuildMetadata.SourceURI;
                sourceFormat = PackageManager.SOURCE_FORMAT_GIT;
                type = protobuildMetadata.PackageType;
                downloadMap = protobuildMetadata.DownloadMap;
                archiveTypeMap = protobuildMetadata.ArchiveTypeMap;
                resolvedHash = protobuildMetadata.ResolvedHash;
                return;
            }

            throw new InvalidOperationException("Unknown package protocol scheme for URI: " + uri);
        }
    }
}

