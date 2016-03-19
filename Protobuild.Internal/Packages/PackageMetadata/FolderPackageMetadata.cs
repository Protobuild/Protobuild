namespace Protobuild
{
    internal class FolderPackageMetadata : IPackageMetadata
    {
        public FolderPackageMetadata(
            string folder,
            string packageType,
            ResolveMetadataDelegate resolve)
        {
            Folder = folder;
            PackageType = packageType;
            Resolve = resolve;
            GetProtobuildPackageBinary = null;
        }

        public string Folder { get; }

        public string PackageType { get; }

        public ResolveMetadataDelegate Resolve { get; }

        public GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }
    }
}