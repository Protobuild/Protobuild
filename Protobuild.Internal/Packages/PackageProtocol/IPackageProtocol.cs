namespace Protobuild.Internal
{
    public interface IPackageProtocol
    {
        string[] Schemes { get; }

        IPackageMetadata ResolveSource(string uri, bool preferCacheLookup, string platform);
    }
}