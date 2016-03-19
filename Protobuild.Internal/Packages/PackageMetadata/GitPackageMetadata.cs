using System;

namespace Protobuild
{
    internal class GitPackageMetadata : IPackageMetadata
    {
        public GitPackageMetadata(
            string cloneURI,
            string gitRef,
            string packageType,
            ResolveMetadataDelegate resolve)
        {
            CloneURI = cloneURI;
            GitRef = gitRef;
            PackageType = packageType;
            Resolve = resolve;
            GetProtobuildPackageBinary = null;
        }

        public string CloneURI { get; }

        public string GitRef { get; }

        public string PackageType { get; }

        public ResolveMetadataDelegate Resolve { get; }

        public GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }
    }
}