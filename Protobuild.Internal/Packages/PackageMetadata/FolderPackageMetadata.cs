namespace Protobuild.Internal
{
    public class FolderPackageMetadata : IPackageMetadata
    {
        public string Folder { get; set; }

        public string PackageType { get; set; }
    }
}