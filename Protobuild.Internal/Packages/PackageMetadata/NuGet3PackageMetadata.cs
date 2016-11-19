namespace Protobuild
{
    internal class NuGet3PackageMetadata : ICachableBinaryPackageMetadata
    {
        public NuGet3PackageMetadata(
            string repositoryIndexUri,
            string packageName,
            string packageType,
            string sourceUri,
            string platform,
            string version,
            string binaryFormat,
            string binaryUri,
            string commitHashForSourceResolve,
            ResolveMetadataDelegate resolve)
        {
            RepositoryIndexUri = repositoryIndexUri;
            PackageName = packageName;
            PackageType = packageType;
            SourceUri = sourceUri;
            Platform = platform;
            Version = version;
            BinaryFormat = binaryFormat;
            BinaryUri = binaryUri;
            CommitHashForSourceResolve = commitHashForSourceResolve;
            Resolve = resolve;
        }

        public string RepositoryIndexUri { get; }

        public string PackageName { get; }

        public string PackageType { get; }

        public ResolveMetadataDelegate Resolve { get; }

        public GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary => null;

        public string SourceUri { get; }

        public string Platform { get; }

        string ICachableBinaryPackageMetadata.CanonicalURI => $"{RepositoryIndexUri}|{PackageName}";

        string ICachableBinaryPackageMetadata.GitCommitOrRef => Version;

        public string Version { get; }

        public string CommitHashForSourceResolve { get; }

        public string BinaryFormat { get; }

        public string BinaryUri { get; }
    }
}
