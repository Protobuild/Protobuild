namespace Protobuild.Internal
{
    internal interface IPackageProtocol
    {
        string[] Schemes { get; }

        IPackageMetadata ResolveSource(PackageRequestRef request);
    }
}