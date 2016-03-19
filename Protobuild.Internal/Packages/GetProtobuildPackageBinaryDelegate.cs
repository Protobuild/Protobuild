namespace Protobuild
{
    internal delegate void GetProtobuildPackageBinaryDelegate(
        IPackageMetadata metadata, out string archiveType, out byte[] packageData);
}