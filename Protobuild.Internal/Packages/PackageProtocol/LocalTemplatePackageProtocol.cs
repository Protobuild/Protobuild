namespace Protobuild.Internal
{
    internal class LocalTemplatePackageProtocol : IPackageProtocol
    {
        private readonly SourcePackageResolve _sourcePackageResolve;

        public LocalTemplatePackageProtocol(SourcePackageResolve sourcePackageResolve)
        {
            _sourcePackageResolve = sourcePackageResolve;
        }

        public string[] Schemes => new[] {"local-template"};

        public IPackageMetadata ResolveSource(PackageRequestRef request)
        {
            return new FolderPackageMetadata(
                request.Uri.Substring("local-template://".Length),
                PackageManager.PACKAGE_TYPE_TEMPLATE,
                (metadata, folder, name, upgrade, source) => _sourcePackageResolve.Resolve(metadata, folder, name, upgrade));
        }
    }
}
