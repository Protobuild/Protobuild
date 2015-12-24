namespace Protobuild.Internal
{
    public class LocalTemplatePackageProtocol : IPackageProtocol
    {
        public string[] Schemes => new[] {"local-template"};

        public IPackageMetadata ResolveSource(string uri, bool preferCacheLookup, string platform)
        {
            return new FolderPackageMetadata
            {
                Folder = uri.Substring("local-template://".Length),
                PackageType = PackageManager.PACKAGE_TYPE_TEMPLATE,
            };
        }
    }
}
