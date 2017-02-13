namespace Protobuild
{
    public interface IPackageMetadata
    {
        string PackageName { get; }

        string PackageType { get; }

        ResolveMetadataDelegate Resolve { get; }

        GetProtobuildPackageBinaryDelegate GetProtobuildPackageBinary { get; }
    }
}