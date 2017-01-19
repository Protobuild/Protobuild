namespace Protobuild
{
    internal interface IPackageResolve
    {
        void Resolve(string workingDirectory, IPackageMetadata metadata, string folder, string templateName, bool forceUpgrade);
    }
}