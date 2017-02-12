namespace Protobuild
{
    internal class ProtobuildPackageMetadata : ICachableBinaryPackageMetadata
    {
        public ProtobuildPackageMetadata(
            string referenceUri,
            string packageType,
            string sourceURI,
            string platform,
            string gitCommit,
            string binaryFormat,
            string binaryUri,
            ResolveMetadataDelegate resolve,
            GetProtobuildPackageBinaryDelegate getProtobuildPackageBinary)
        {
            ReferenceURI = referenceUri;
            PackageType = packageType;
            SourceURI = sourceURI;
            Platform = platform;
            GitCommit = gitCommit;
            BinaryFormat = binaryFormat;
            BinaryUri = binaryUri;
            Resolve = resolve;
            GetProtobuildPackageBinary = getProtobuildPackageBinary;
        }

        public string ReferenceURI { get; }

        public string PackageName => null;

        public string PackageType { get; }

        public ResolveMetadataDelegate Resolve { get; }

        public GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }

        public string SourceURI { get; }

        public string Platform { get; }

        string ICachableBinaryPackageMetadata.CanonicalURI => ReferenceURI;

        string ICachableBinaryPackageMetadata.GitCommitOrRef => GitCommit;

        public string GitCommit { get; }

        public string BinaryFormat { get; }

        public string BinaryUri { get; }
    }
}
