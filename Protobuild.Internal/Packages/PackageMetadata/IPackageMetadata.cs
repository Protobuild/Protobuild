namespace Protobuild
{
    internal interface IPackageMetadata
    {
        string PackageType { get; }

        ResolveMetadataDelegate Resolve { get; }

        GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }
    }
}