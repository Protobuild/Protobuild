namespace Protobuild.Internal
{
    internal interface IPackageProtocol
    {
        string[] Schemes { get; }

        IPackageMetadata ResolveSource(string workingDirectory, PackageRequestRef request);
    }
}