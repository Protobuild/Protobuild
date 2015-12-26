namespace Protobuild
{
    public class ProtobuildPackageMetadata : IPackageMetadata
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
            BinaryURI = binaryUri;
            Resolve = resolve;
            GetProtobuildPackageBinary = getProtobuildPackageBinary;
        }

        public string ReferenceURI { get; }

        public string PackageType { get; }

        public ResolveMetadataDelegate Resolve { get; }

        public GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }

        public string SourceURI { get; }

        public string Platform { get; }

        public string GitCommit { get; }

        public string BinaryFormat { get; }

        public string BinaryURI { get; }
    }
}
