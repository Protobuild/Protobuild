namespace Protobuild.Internal
{
    public interface IPackageProtocol
    {
        string[] Schemes { get; }

        IPackageMetadata ResolveSource(PackageRequestRef request);
    }
}