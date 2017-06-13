using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using fastJSON;

namespace Protobuild.Internal
{
    internal class NuGet3PackageProtocol : IPackageProtocol
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;
        private readonly IPackageCacheConfiguration _packageCacheConfiguration;
        private readonly IPackageRequestCache _packageRequestCache;
        private readonly BinaryPackageResolve _binaryPackageResolve;
        private readonly SourcePackageResolve _sourcePackageResolve;

        public NuGet3PackageProtocol(
            IHostPlatformDetector hostPlatformDetector,
            IPackageCacheConfiguration packageCacheConfiguration,
            IPackageRequestCache packageRequestCache,
            BinaryPackageResolve binaryPackageResolve,
            SourcePackageResolve sourcePackageResolve)
        {
            _hostPlatformDetector = hostPlatformDetector;
            _packageCacheConfiguration = packageCacheConfiguration;
            _packageRequestCache = packageRequestCache;
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

        public IPackageMetadata ResolveSource(string workingDirectory, PackageRequestRef request)
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
            var serviceIndex = _packageRequestCache.TryGetOptionallyCachedJsonObject(
                repository,
                !request.ForceUpgrade,
                out serviceJson,
                id => GetData(new Uri(id)));

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
            var packageMetadata = _packageRequestCache.TryGetOptionallyCachedJsonObject(
                packageMetadataUrl,
                !request.ForceUpgrade && request.IsStaticReference,
                out packageMetadataJson,
                id => GetData(new Uri(id)));

            string latestPackageVersion = null;
            foreach (var item in packageMetadata.items)
            {
                if (latestPackageVersion == null || string.CompareOrdinal((string)item.upper, latestPackageVersion) > 0)
                {
                    latestPackageVersion = item.upper;
                }
            }

            var packagesByVersionLock = new object();
            var packagesByVersion = new Dictionary<string, dynamic>();

            if (_hostPlatformDetector.DetectPlatform() == "Windows")
            {
                // Do this in parallel as we may need to make multiple HTTPS requests.
                Parallel.ForEach((IEnumerable<object>)packageMetadata.items,
                    item => PopulatePackagesByVersion(packagesByVersionLock, packagesByVersion, (dynamic)item, request));
            }
            else
            {
                // Parallelisation is not safe on this platform, do it sequentually.
                foreach (var item in packageMetadata.items)
                {
                    PopulatePackagesByVersion(packagesByVersionLock, packagesByVersion, item, request);
                }
            }

            string packageType = PackageManager.PACKAGE_TYPE_LIBRARY;

            string sourceCodeUrl = null;
            string commitHashForSourceResolve = null;
            if (shouldQuerySourceRepository)
            {
                var lookupVersion = latestPackageVersion;
                if (!packagesByVersion.ContainsKey(lookupVersion) && packagesByVersion.ContainsKey(lookupVersion + "+git.unspecified"))
                {
                    lookupVersion += "+git.unspecified";
                }
                sourceCodeUrl = ExtractSourceRepository(packagesByVersion[lookupVersion]);
                packageType = ExtractPackageType(packagesByVersion[lookupVersion]);

                if (!string.IsNullOrWhiteSpace(sourceCodeUrl))
                {
                    if (sourceCodeUrl.StartsWith("git="))
                    {
                        sourceCodeUrl = sourceCodeUrl.Substring("git=".Length);

                        var performGitLsRemote = true;
                        if (sourceCodeUrl.StartsWith("https://github.com/"))
                        {
                            try
                            {
                                // This is a GitHub repository.  During the installation of Protobuild Manager (hosted on GitHub), we need
                                // to resolve the latest version on NuGet, but we can't do this because Git may not be in the PATH or may
                                // not be available.  We still want developers to be able to install Protobuild Manager without Git on
                                // their PATH (as they may have dedicated shells to use it), so we attempt to use the GitHub API to resolve
                                // the commit hash first.
                                string gitHubJsonInfo;
                                var gitHubComponents =
                                    sourceCodeUrl.Substring("https://github.com/".Length).Split('/');
                                var gitHubOwner = gitHubComponents[0];
                                var gitHubRepo = gitHubComponents[1];
                                var gitHubApiUrl = "https://api.github.com/repos/" + gitHubOwner +
                                             "/" +
                                             gitHubRepo + "/branches/" + version;
                                var gitHubJson = _packageRequestCache.TryGetOptionallyCachedJsonObject(
                                    gitHubApiUrl,
                                    !request.ForceUpgrade && request.IsStaticReference,
                                    out packageMetadataJson,
                                    id =>
                                    {
                                        using (var client = new RetryableWebClient())
                                        {
                                            client.SilentOnError = true;
                                            client.SetHeader("User-Agent", "Protobuild NuGet Lookup/v1.0");
                                            client.SetHeader("Accept", "application/vnd.github.v3+json");

                                            return client.DownloadString(gitHubApiUrl);
                                        }
                                    });
                                var commitHash = gitHubJson.commit.sha;

                                if (!string.IsNullOrWhiteSpace(commitHash))
                                {
                                    // This is a match and we've found our Git hash to use.
                                    version =
                                        NuGetVersionHelper.CreateNuGetPackageVersion(commitHash.Trim(),
                                            request.Platform);
                                    commitHashForSourceResolve = commitHash.Trim();
                                    performGitLsRemote = false;
                                }
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("NOTICE: Unable to lookup version information via GitHub API; falling back to 'git ls-remote'");
                            }
                        }

                        if (performGitLsRemote)
                        {
                            var heads = _packageRequestCache.TryGetOptionallyCachedData(
                                "git:" + sourceCodeUrl,
                                !request.ForceUpgrade && request.IsStaticReference,
                                id => GitUtils.RunGitAndCapture(
                                    workingDirectory,
                                    null,
                                    "ls-remote --heads " + new Uri(sourceCodeUrl)));

                            var lines = heads.Split(new string[] {"\r\n", "\n", "\r"},
                                StringSplitOptions.RemoveEmptyEntries);

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
                                            NuGetVersionHelper.CreateNuGetPackageVersion(
                                                sourceEntryComponents[0].Trim(),
                                                request.Platform);
                                        commitHashForSourceResolve = sourceEntryComponents[0].Trim();
                                        break;
                                    }
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

                // packageContent may not be under catalogEntry; our best guess is that NuGet
                // moved it from the root to catalogEntry at some point in the past, but not
                // all of the registrations are updated with it under catalogEntry, e.g. RestSharp
                try
                {
                    binaryUri = packagesByVersion[version].catalogEntry.packageContent;
                }
                catch
                {
                    binaryUri = packagesByVersion[version].packageContent;
                }

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
                (workingDirectoryAlt, metadata, folder, name, upgrade, source) =>
                {
                    if (source == true)
                    {
                        _sourcePackageResolve.Resolve(workingDirectoryAlt, metadata, folder, name, upgrade);
                    }
                    else
                    {
                        _binaryPackageResolve.Resolve(workingDirectoryAlt, metadata, folder, name, upgrade);
                    }
                });
        }

        private void PopulatePackagesByVersion(object packagesByVersionLock, Dictionary<string, object> packagesByVersion, dynamic item, PackageRequestRef request)
        {
            try
            {
                var subitems = item.items;
                lock (packagesByVersionLock)
                {
                    foreach (var subitem in subitems)
                    {
                        packagesByVersion[(string) subitem.catalogEntry.version] = subitem;
                    }
                }
            }
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                string subdocumentJson;
                dynamic subdocument;

                // We can cache this request even when we have non-static references if the cached
                // document has 64 items in it.  This is because individual documents can only at
                // maximum have 64 items in them, so adding a version between two points will cause
                // new URLs to be generated (that won't have previously been cached).
                var idUrl = (string)item["@id"];
                if (!request.ForceUpgrade && _packageRequestCache.IsCached(idUrl))
                {
                    try
                    {
                        subdocument = _packageRequestCache.GetCachedJsonObject(idUrl);
                        if ((int)subdocument.count == 64)
                        {
                            // Use the persisted version in the cache since it will never change.
                            lock (packagesByVersionLock)
                            {
                                foreach (var subitem in subdocument.items)
                                {
                                    packagesByVersion[(string) subitem.catalogEntry.version] = subitem;
                                }
                            }
                            return;
                        }
                    }
                    catch
                    {
                        // Unable to parse or read cached data.  Fallback to making the request again.
                    }
                }

                // When NuGet packages have a lot of versions, the items list is in a seperate document.
                // Download the document for each group of versions.  Ideally we would only download
                // the document we need, but for branches it's a little more complicated (we first need
                // to get the document for the latest version and then get the document for the resolved
                // version).  For now, we just download each document as we need it.
                subdocument = _packageRequestCache.TryGetOptionallyCachedJsonObject(
                    idUrl,
                    !request.ForceUpgrade && request.IsStaticReference,
                    out subdocumentJson,
                    id => GetData(new Uri(id)));
                lock (packagesByVersionLock)
                {
                    foreach (var subitem in subdocument.items)
                    {
                        packagesByVersion[(string) subitem.catalogEntry.version] = subitem;
                    }
                }
            }
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

        private dynamic GetData(Uri indexUri)
        {
            using (var client = new RetryableWebClient())
            {
                return client.DownloadString(indexUri);
            }
        }

        private dynamic GetJSON(Uri indexUri, out string str)
        {
            str = GetData(indexUri);
            return JSON.ToDynamic(str);
        }
    }
}
