namespace Protobuild
{
    public class TransformedPackageMetadata : IPackageMetadata
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

        public string GitRef { get; }

        public IPackageTransformer Transformer { get; }

        public ResolveMetadataDelegate Resolve { get; }

        public GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }

        public string SourceURI { get; }
    }
}
