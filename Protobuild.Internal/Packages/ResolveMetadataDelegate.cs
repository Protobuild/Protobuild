namespace Protobuild
{
    internal delegate void ResolveMetadataDelegate(
        IPackageMetadata metadata, string folder, string templateName, bool forceUpgrade, bool? preferSource);
}