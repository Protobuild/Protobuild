namespace Protobuild
{
    internal class TransformedPackageMetadata : ICachableBinaryPackageMetadata
    {
        public TransformedPackageMetadata(
            string sourceUri, 
            string packageType,
            string platform,
            string gitRef,
            IPackageTransformer transformer,
            ResolveMetadataDelegate resolve,
            GetProtobuildPackageBinaryDelegate getProtobuildPackageBinary)
        {
            SourceURI = sourceUri;
            PackageType = packageType;
            Platform = platform;
            GitRef = gitRef;
            Transformer = transformer;
            Resolve = resolve;
            GetProtobuildPackageBinary = getProtobuildPackageBinary;
        }

        public string PackageType { get; }

        public string Platform { get; }

        string ICachableBinaryPackageMetadata.CanonicalURI => SourceURI;

        string ICachableBinaryPackageMetadata.GitCommitOrRef => GitRef;

        string ICachableBinaryPackageMetadata.BinaryFormat => PackageManager.ARCHIVE_FORMAT_TAR_LZMA;

        public string GitRef { get; }

        public IPackageTransformer Transformer { get; }

        public ResolveMetadataDelegate Resolve { get; }

        public GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }

        public string SourceURI { get; }
    }
}
