namespace Protobuild
{
    public delegate void ResolveMetadataDelegate(
        string workingDirectory, IPackageMetadata metadata, string folder, string templateName, bool forceUpgrade, bool? preferSource);
}