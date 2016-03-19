namespace Protobuild
{
    internal interface IPackageResolve
    {
        void Resolve(IPackageMetadata metadata, string folder, string templateName, bool forceUpgrade);
    }
}