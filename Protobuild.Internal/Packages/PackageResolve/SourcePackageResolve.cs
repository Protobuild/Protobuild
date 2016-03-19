using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Protobuild
{
    internal class SourcePackageResolve : IPackageResolve
    {
        private readonly IProjectTemplateApplier _projectTemplateApplier;
        private readonly IPackageCacheConfiguration _packageCacheConfiguration;

        public SourcePackageResolve(
            IProjectTemplateApplier projectTemplateApplier,
            IPackageCacheConfiguration packageCacheConfiguration)
        {
            _projectTemplateApplier = projectTemplateApplier;
            _packageCacheConfiguration = packageCacheConfiguration;
        }

        public void Resolve(IPackageMetadata metadata, string folder, string templateName, bool forceUpgrade)
        {
            var gitMetadata = metadata as GitPackageMetadata;
            var protobuildMetadata = metadata as ProtobuildPackageMetadata;
            var folderMetadata = metadata as FolderPackageMetadata;

            if (gitMetadata != null)
            {
                ResolveGit(gitMetadata, folder, templateName, forceUpgrade);
                return;
            }

            if (protobuildMetadata != null)
            {
                ResolveProtobuild(protobuildMetadata, folder, templateName, forceUpgrade);
                return;
            }

            if (folderMetadata != null)
            {
                ResolveFolder(folderMetadata, folder, templateName);
                return;
            }

            throw new InvalidOperationException("Unexpected metadata type " + metadata.GetType().Name + " for source resolve.");
        }

        private void ResolveProtobuild(ProtobuildPackageMetadata protobuildMetadata, string folder, string templateName, bool forceUpgrade)
        {
            ResolveGit(
                new GitPackageMetadata(
                    protobuildMetadata.SourceURI,
                    protobuildMetadata.GitCommit,
                    protobuildMetadata.PackageType,
                    (metadata, s, name, upgrade, source) => Resolve(metadata, s, name, upgrade)
                    ),
                folder,
                templateName,
                forceUpgrade);
        }

        private void ResolveGit(GitPackageMetadata gitMetadata, string folder, string templateName, bool forceUpgrade)
        {
            switch (gitMetadata.PackageType)
            {
                case PackageManager.PACKAGE_TYPE_LIBRARY:
                    if (File.Exists(Path.Combine(folder, ".git")) || Directory.Exists(Path.Combine(folder, ".git")))
                    {
                        if (!forceUpgrade)
                        {
                            Console.WriteLine("Git submodule / repository already present at " + folder);
                            return;
                        }
                    }

                    PathUtils.AggressiveDirectoryDelete(folder);

                    var packageLibrary = GetSourcePackage(gitMetadata.CloneURI);
                    ExtractGitSourceTo(packageLibrary, gitMetadata.GitRef, folder);
                    break;
                case PackageManager.PACKAGE_TYPE_TEMPLATE:
                    if (Directory.Exists(".staging"))
                    {
                        PathUtils.AggressiveDirectoryDelete(".staging");
                    }

                    var packageTemplate = GetSourcePackage(gitMetadata.CloneURI);
                    ExtractGitSourceTo(packageTemplate, gitMetadata.GitRef, ".staging");

                    _projectTemplateApplier.Apply(".staging", templateName);
                    PathUtils.AggressiveDirectoryDelete(".staging");
                    break;
                default:
                    throw new InvalidOperationException("Unable to resolve source package with type '" + gitMetadata.PackageType + "' using Git-based package.");
            }
        }

        private void ResolveFolder(FolderPackageMetadata folderMetadata, string folder, string templateName)
        {
            switch (folderMetadata.PackageType)
            {
                case PackageManager.PACKAGE_TYPE_TEMPLATE:
                    if (folder != string.Empty)
                    {
                        throw new InvalidOperationException("Reference folder must be empty for template type.");
                    }

                    _projectTemplateApplier.Apply(folderMetadata.Folder, templateName);
                    break;
                default:
                    throw new InvalidOperationException("Unable to resolve source package with type '" + folderMetadata.PackageType + "' using folder-based package.");
            }
        }

        private bool HasSourcePackage(string url)
        {
            var sourceName = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(url));
            if (Directory.Exists(sourceName))
            {
                return true;
            }

            return false;
        }

        private string GetSourcePackage(string url)
        {
            var sourcePath = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(url));

            if (this.HasSourcePackage(url))
            {
                if (Directory.Exists(Path.Combine(sourcePath, "objects")) &&
                    File.Exists(Path.Combine(sourcePath, "config")))
                {
                    try
                    {
                        GitUtils.RunGitAbsolute(sourcePath, "fetch origin +refs/heads/*:refs/heads/*");
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore exceptions here in case the user is offline.
                    }

                    return sourcePath;
                }
                else
                {
                    Console.Error.WriteLine("WARNING: Source package cache is corrupt, removing and cloning again...");
                    try
                    {
                        Directory.Delete(sourcePath, true);
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine("WARNING: Unable to delete invalid source package from cache!");
                    }
                }
            }

            Directory.CreateDirectory(sourcePath);
            GitUtils.RunGit(null, "clone --progress --bare " + url + " \"" + sourcePath + "\"");

            return sourcePath;
        }

        private string GetPackageName(string url)
        {
            var sha1 = new SHA1Managed();

            var urlHashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(NormalizeURIForCache(url)));
            var urlHashString = BitConverter.ToString(urlHashBytes).Replace("-", "").ToLowerInvariant();

            return urlHashString + "--source";
        }

        private string NormalizeURIForCache(string canonicalUri)
        {
            var index = canonicalUri.IndexOf("://", StringComparison.InvariantCulture);

            if (index != -1)
            {
                return canonicalUri.Substring(index + "://".Length);
            }

            return canonicalUri;
        }

        private void ExtractGitSourceTo(string sourcePath, string gitRef, string path)
        {
            // FIXME: This assumes packages are being extracted underneath the current
            // working directory (i.e. the module root).
            if (GitUtils.IsGitRepository())
            {
                GitUtils.UnmarkIgnored(path);
            }

            GitUtils.RunGit(null, "clone --progress " + sourcePath + " \"" + path + "\"");
            GitUtils.RunGit(path, "checkout -f " + gitRef);
            this.InitializeSubmodulesFromCache(path);

            if (GitUtils.IsGitRepository())
            {
                GitUtils.MarkIgnored(path);
            }
        }

        private void InitializeSubmodulesFromCache(string path)
        {
            GitUtils.RunGit(path, "submodule init");
            var submodules = GitUtils.RunGitAndCapture(path, "config --local --list");
            foreach (Match match in new Regex(@"submodule\.(?<name>.*)\.url=(?<url>.*)").Matches(submodules))
            {
                var name = match.Groups["name"].Value;
                var url = match.Groups["url"].Value;

                var submodule = GetSourcePackage(url);
                GitUtils.RunGit(path, "config --local submodule." + name + ".url " + submodule);
                GitUtils.RunGit(path, "submodule update " + name);
                this.InitializeSubmodulesFromCache(Path.Combine(path ?? "", name));
                GitUtils.RunGit(path, "config --local submodule." + name + ".url " + url);
            }
        }
    }
}