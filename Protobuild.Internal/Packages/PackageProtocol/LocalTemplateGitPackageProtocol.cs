namespace Protobuild.Internal
{
    internal class LocalTemplateGitPackageProtocol : IPackageProtocol
    {
        private readonly SourcePackageResolve _sourcePackageResolve;

        public LocalTemplateGitPackageProtocol(SourcePackageResolve sourcePackageResolve)
        {
            _sourcePackageResolve = sourcePackageResolve;
        }

        public string[] Schemes => new[] { "local-template-git" };

        public IPackageMetadata ResolveSource(string workingDirectory, PackageRequestRef request)
        {
            return new GitPackageMetadata(
                request.Uri.Substring("local-template-git://".Length),
                request.GitRef,
                PackageManager.PACKAGE_TYPE_TEMPLATE,
                (workingDirectoryAlt, metadata, folder, name, upgrade, source) => _sourcePackageResolve.Resolve(workingDirectoryAlt, metadata, folder, name, upgrade));
        }
    }
}
