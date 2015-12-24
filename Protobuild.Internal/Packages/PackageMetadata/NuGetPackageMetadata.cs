namespace Protobuild.Internal
{
    public class NuGetPackageMetadata : IPackageMetadata
    {
        public string PackageType { get; set; }
        public string SourceURI { get; set; }
    }
}
