namespace Protobuild
{
    public class NuGetPackageMetadata : IPackageMetadata
    {
        public NuGetPackageMetadata(
            string sourceUri, 
            string packageType,
            ResolveMetadataDelegate resolve,
            GetProtobuildPackageBinaryDelegate getProtobuildPackageBinary)
        {
            SourceURI = sourceUri;
            PackageType = packageType;
            Resolve = resolve;
            GetProtobuildPackageBinary = getProtobuildPackageBinary;
        }

        public string PackageType { get; }

        public ResolveMetadataDelegate Resolve { get; }

        public GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }

        public string SourceURI { get; }
    }
}
