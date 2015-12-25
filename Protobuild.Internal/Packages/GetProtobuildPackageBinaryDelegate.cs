namespace Protobuild
{
    public delegate void GetProtobuildPackageBinaryDelegate(
        IPackageMetadata metadata, out string archiveType, out byte[] packageData);
}