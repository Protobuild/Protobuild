namespace Protobuild.Internal
{
    public class LocalTemplateGitPackageProtocol : IPackageProtocol
    {
        public string[] Schemes => new[] { "local-template-git" };

        public IPackageMetadata ResolveSource(string uri, bool preferCacheLookup, string platform)
        {
            return new GitPackageMetadata
            {
                CloneURI = uri.Substring("local-template-git://".Length),
                PackageType = PackageManager.PACKAGE_TYPE_TEMPLATE,
            };
        }
    }
}
