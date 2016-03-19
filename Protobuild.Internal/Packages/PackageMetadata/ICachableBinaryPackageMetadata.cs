namespace Protobuild
{
    internal interface ICachableBinaryPackageMetadata : IPackageMetadata
    {
        string Platform { get; }

        string CanonicalURI { get; }

        string GitCommitOrRef { get; }

        string BinaryFormat { get; }
    }
}