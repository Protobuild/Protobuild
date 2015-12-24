namespace Protobuild.Internal
{
    public class GitPackageMetadata : IPackageMetadata
    {
        public string CloneURI { get; set; }

        public string PackageType { get; set; }
    }
}