namespace Protobuild
{
    internal interface ICachableBinaryPackageMetadata : IBinaryPackageMetadata
    {
        string Platform { get; }

        string CanonicalURI { get; }

        string GitCommitOrRef { get; }
    }

    internal interface IBinaryPackageMetadata : IPackageMetadata
    {
        string BinaryUri { get; }

        string BinaryFormat { get; }
    }
}