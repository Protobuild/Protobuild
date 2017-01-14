using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using fastJSON;

namespace Protobuild.Internal
{
    internal class NuGet3PackageProtocol : IPackageProtocol
    {
        private readonly IPackageCacheConfiguration _packageCacheConfiguration;
        private readonly BinaryPackageResolve _binaryPackageResolve;
        private readonly SourcePackageResolve _sourcePackageResolve;

        public NuGet3PackageProtocol(
            IPackageCacheConfiguration packageCacheConfiguration,
            BinaryPackageResolve binaryPackageResolve,
            SourcePackageResolve sourcePackageResolve)
        {
            _packageCacheConfiguration = packageCacheConfiguration;
            _binaryPackageResolve = binaryPackageResolve;
            _sourcePackageResolve = sourcePackageResolve;
        }

        public string[] Schemes => new[] {"http-nuget-v3", "https-nuget-v3" };

        private string NormalizeScheme(string uri)
        {
            if (uri.StartsWith("http-nuget-v3://"))
            {
                return "http://" + uri.Substring("http-nuget-v3://".Length);
            }

            if (uri.StartsWith("https-nuget-v3://"))
            {
                return "https://" + uri.Substring("https-nuget-v3://".Length);
            }

            throw new InvalidOperationException("Unexpected scheme!");
        }

        public IPackageMetadata ResolveSource(PackageRequestRef request)
        {
            var components = request.Uri.Split(new[] {'|'}, 2);

            var repository = NormalizeScheme(components[0]);
            var packageName = components[1];
            var version = request.GitRef;

            var semVerRegex = new Regex("^[0-9]\\.[0-9]\\.[0-9](\\-.*)?$");
            var gitHashRegex = new Regex("^[0-9a-fA-F]{40}$");

            var shouldQuerySourceRepository = false;
            if (semVerRegex.IsMatch(version))
            {
                // This is a semantic version; leave as-is.
            }
            else if (gitHashRegex.IsMatch(version))
            {
                // This is a Git hash, convert it to NuGet's semantic version.
                version = NuGetVersionHelper.CreateNuGetPackageVersion(version, request.Platform);
            }
            else
            {
                // This is a branch, or other kind of source reference.  We need
                // to query the source repository to resolve it to a Git hash.
                shouldQuerySourceRepository = true;
            }

            string serviceJson;
            var serviceIndex = GetJSON(new Uri(repository), out serviceJson);

            string registrationsBaseUrl = null;

            foreach (var entry in serviceIndex.resources)
            {
                var entryType = (string) entry["@type"];
                if (entryType == "RegistrationsBaseUrl")
                {
                    registrationsBaseUrl = (string) entry["@id"];
                }
            }

            if (registrationsBaseUrl == null)
            {
                throw new InvalidOperationException("Unable to locate RegistrationsBaseUrl service.");
            }

            var packageMetadataUrl =
                $"{registrationsBaseUrl.TrimEnd(new[] {'/'})}/{packageName.ToLowerInvariant()}/index.json";

            string packageMetadataJson;
            var packageMetadata = GetJSON(new Uri(packageMetadataUrl), out packageMetadataJson);

            string latestPackageVersion = null;
            foreach (var item in packageMetadata.items)
            {
                if (latestPackageVersion == null || string.CompareOrdinal((string)item.upper, latestPackageVersion) > 0)
                {
                    latestPackageVersion = item.upper;
                }
            }

            var packagesByVersion = new Dictionary<string, dynamic>();
            foreach (var item in packageMetadata.items)
            {
                try
                {
                    var subitems = item.items;
                    foreach (var subitem in subitems)
                    {
                        packagesByVersion[(string)subitem.catalogEntry.version] = subitem;
                    }
                }
                catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                {
                    // When NuGet packages have a lot of versions, the items list is in a seperate document.
                    // Download the document for each group of versions.  Ideally we would only download
                    // the document we need, but for branches it's a little more complicated (we first need
                    // to get the document for the latest version and then get the document for the resolved
                    // version).  For now, we just download each document as we need it.
                    string subdocumentJson;
                    var subdocument = GetJSON(new Uri((string)item["@id"]), out subdocumentJson);
                    foreach (var subitem in subdocument.items)
                    {
                        packagesByVersion[(string)subitem.catalogEntry.version] = subitem;
                    }
                }
            }

            string packageType = PackageManager.PACKAGE_TYPE_LIBRARY;

            string sourceCodeUrl = null;
            string commitHashForSourceResolve = null;
            if (shouldQuerySourceRepository)
            {
                sourceCodeUrl = ExtractSourceRepository(packagesByVersion[latestPackageVersion]);
                packageType = ExtractPackageType(packagesByVersion[latestPackageVersion]);

                if (!string.IsNullOrWhiteSpace(sourceCodeUrl))
                {
                    if (sourceCodeUrl.StartsWith("git="))
                    {
                        sourceCodeUrl = sourceCodeUrl.Substring("git=".Length);

                        var heads = GitUtils.RunGitAndCapture(
                            Environment.CurrentDirectory,
                            "ls-remote --heads " + new Uri(sourceCodeUrl));

                        var lines = heads.Split(new string[] {"\r\n", "\n", "\r"}, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                        {
                            var sourceEntryComponents = line.Split('\t');
                            if (sourceEntryComponents.Length >= 2)
                            {
                                var branchName = sourceEntryComponents[1].Trim();

                                if (branchName.StartsWith("refs/heads/"))
                                {
                                    branchName = branchName.Substring("refs/heads/".Length);
                                }
                                else
                                {
                                    continue;
                                }

                                if (string.Equals(version, branchName, StringComparison.InvariantCulture))
                                {
                                    // This is a match and we've found our Git hash to use.
                                    version =
                                        NuGetVersionHelper.CreateNuGetPackageVersion(sourceEntryComponents[0].Trim(),
                                            request.Platform);
                                    commitHashForSourceResolve = sourceEntryComponents[0].Trim();
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown source code repository type '" + sourceCodeUrl + "'");
                    }

                    // If we fall out of this loop, we'll hit the next if statement and most likely fail.
                }
            }

            string binaryUri = null;
            string binaryFormat = null;
            if (!packagesByVersion.ContainsKey(version))
            {
                if (string.IsNullOrWhiteSpace(sourceCodeUrl))
                {
                    throw new InvalidOperationException(
                        "Unable to resolve binary package for version \"" +
                        version + "\" and platform \"" + request.Platform +
                        "\" and this package does not have a source repository");
                }
                else
                {
                    RedirectableConsole.WriteLine("Unable to resolve binary package for version \"" + version +
                                      "\" and platform \"" + request.Platform + "\", falling back to source version");
                }
            }
            else
            {
                sourceCodeUrl = ExtractSourceRepository(packagesByVersion[version]);
                packageType = ExtractPackageType(packagesByVersion[version]);

                if (!string.IsNullOrWhiteSpace(sourceCodeUrl))
                {
                    if (sourceCodeUrl.StartsWith("git="))
                    {
                        sourceCodeUrl = sourceCodeUrl.Substring("git=".Length);
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown source code repository type '" + sourceCodeUrl + "'");
                    }
                }

                if (commitHashForSourceResolve == null)
                {
                    commitHashForSourceResolve = ExtractCommitHash(packagesByVersion[version]);
                }

                binaryUri = packagesByVersion[version].catalogEntry.packageContent;
                binaryFormat = PackageManager.ARCHIVE_FORMAT_NUGET_ZIP;
            }
            
            return new NuGet3PackageMetadata(
                repository,
                packageName,
                packageType,
                sourceCodeUrl,
                request.Platform,
                version,
                binaryFormat,
                binaryUri,
                commitHashForSourceResolve,
                (metadata, folder, name, upgrade, source) =>
                {
                    if (source == true)
                    {
                        _sourcePackageResolve.Resolve(metadata, folder, name, upgrade);
                    }
                    else
                    {
                        _binaryPackageResolve.Resolve(metadata, folder, name, upgrade);
                    }
                });
        }

        private string ExtractSourceRepository(dynamic versionMetadata)
        {
            foreach (var tagObj in versionMetadata.catalogEntry.tags)
            {
                var tag = (string) tagObj;
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    // NOTE: Add additional entries here as we support more source control systems.
                    if (tag.StartsWith("git="))
                    {
                        return tag;
                    }
                }
            }

            return null;
        }

        private string ExtractCommitHash(dynamic versionMetadata)
        {
            foreach (var tagObj in versionMetadata.catalogEntry.tags)
            {
                var tag = (string)tagObj;
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    if (tag.StartsWith("commit="))
                    {
                        return tag.Substring("commit=".Length);
                    }
                }
            }

            return null;
        }

        private string ExtractPackageType(dynamic versionMetadata)
        {
            foreach (var tagObj in versionMetadata.catalogEntry.tags)
            {
                var tag = (string)tagObj;
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    if (tag == "type=library")
                    {
                        return PackageManager.PACKAGE_TYPE_LIBRARY;
                    }

                    if (tag == "type=global-tool")
                    {
                        return PackageManager.PACKAGE_TYPE_GLOBAL_TOOL;
                    }

                    if (tag == "type=template")
                    {
                        return PackageManager.PACKAGE_TYPE_TEMPLATE;
                    }
                }
            }

            // Defaults to library.
            return PackageManager.PACKAGE_TYPE_LIBRARY;
        }

        private dynamic GetJSON(Uri indexUri, out string str)
        {
            using (var client = new RetryableWebClient())
            {
                str = client.DownloadString(indexUri);
                return JSON.ToDynamic(str);
            }
        }
    }
}
