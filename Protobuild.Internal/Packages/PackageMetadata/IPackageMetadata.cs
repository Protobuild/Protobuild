namespace Protobuild
{
    public interface IPackageMetadata
    {
        string PackageType { get; }

        ResolveMetadataDelegate Resolve { get; }

        GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }
    }
}