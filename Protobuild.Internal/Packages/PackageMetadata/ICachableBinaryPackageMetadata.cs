namespace Protobuild
{
    public interface ICachableBinaryPackageMetadata : IPackageMetadata
    {
        string Platform { get; }

        string CanonicalURI { get; }

        string GitCommitOrRef { get; }

        string BinaryFormat { get; }
    }
}