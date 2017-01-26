namespace Protobuild
{
    internal delegate void ResolveMetadataDelegate(
        string workingDirectory, IPackageMetadata metadata, string folder, string templateName, bool forceUpgrade, bool? preferSource);
}