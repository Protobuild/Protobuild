using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using fastJSON;

namespace Protobuild.Internal
{
    internal class ProtobuildPackageProtocol : IPackageProtocol
    {
        private readonly IPackageCacheConfiguration _packageCacheConfiguration;
        private readonly IPackageRequestCache _packageRequestCache;
        private readonly BinaryPackageResolve _binaryPackageResolve;
        private readonly SourcePackageResolve _sourcePackageResolve;

        public ProtobuildPackageProtocol(
            IPackageCacheConfiguration packageCacheConfiguration,
            IPackageRequestCache packageRequestCache,
            BinaryPackageResolve binaryPackageResolve,
            SourcePackageResolve sourcePackageResolve)
        {
            _packageCacheConfiguration = packageCacheConfiguration;
            _packageRequestCache = packageRequestCache;
            _binaryPackageResolve = binaryPackageResolve;
            _sourcePackageResolve = sourcePackageResolve;
        }

        public string[] Schemes => new[] {"http", "https" };

        public IPackageMetadata ResolveSource(string workingDirectory, PackageRequestRef request)
        {
            var baseUri = new Uri(request.Uri);

            var apiUri = new Uri(baseUri.ToString().TrimEnd('/') + "/api");
            dynamic apiData = null;

            var performOnlineLookup = true;
            if (!request.ForceUpgrade && request.IsStaticReference)
            {
                performOnlineLookup = false;
                if (_packageRequestCache.IsCached(request.Uri))
                {
                    try
                    {
                        apiData = _packageRequestCache.GetCachedJsonObject(request.Uri);
                    }
                    catch (ExecEnvironment.SelfInvokeExitException)
                    {
                        throw;
                    }
                    catch
                    {
                        performOnlineLookup = true;
                    }
                }
                else
                {
                    performOnlineLookup = true;
                }
            }

            if (performOnlineLookup)
            {
                try
                {
                    string jsonString;
                    apiData = this.GetJSON(apiUri, out jsonString);
                    if (apiData.has_error)
                    {
                        throw new InvalidOperationException((string)apiData.error);
                    }
                    try
                    {
                        _packageRequestCache.StoreCachedData(request.Uri, jsonString);
                    }
                    catch (IOException)
                    {
                        RedirectableConsole.WriteLine("WARNING: Unable to save cached result of request.");
                    }
                }
                catch (Exception)
                {
                    // Attempt to retrieve it from the lookup cache.
                    if (_packageRequestCache.IsCached(request.Uri))
                    {
                        var shouldThrow = false;
                        try
                        {
                            apiData = _packageRequestCache.GetCachedJsonObject(request.Uri);
                        }
                        catch (ExecEnvironment.SelfInvokeExitException)
                        {
                            throw;
                        }
                        catch
                        {
                            shouldThrow = true;
                        }
                        if (shouldThrow)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            if (apiData == null)
            {
                throw new InvalidOperationException("apiData is null");
            }
            
            var sourceUri = (string)apiData.result.package.gitUrl;
            var type = (string)apiData.result.package.type;

            if (!string.IsNullOrWhiteSpace(sourceUri))
            {
                try
                {
                    new Uri(sourceUri);
                }
                catch (ExecEnvironment.SelfInvokeExitException)
                {
                    throw;
                }
                catch
                {
                    throw new InvalidOperationException(
                        "Received invalid Git URL when loading package from " + apiUri);
                }
            }
            else
            {
                RedirectableConsole.WriteLine("WARNING: This package does not have a source repository set.");
            }

            var downloadMap = new Dictionary<string, string>();
            var archiveTypeMap = new Dictionary<string, string>();
            var resolvedHash = new Dictionary<string, string>();
            foreach (var ver in apiData.result.versions)
            {
                if (ver.platformName != request.Platform)
                {
                    continue;
                }
                if (!downloadMap.ContainsKey(ver.versionName))
                {
                    downloadMap.Add(ver.versionName, ver.downloadUrl);
                    archiveTypeMap.Add(ver.versionName, ver.archiveType);
                    resolvedHash.Add(ver.versionName, ver.versionName);
                }
            }
            foreach (var branch in apiData.result.branches)
            {
                if (!downloadMap.ContainsKey(branch.versionName))
                {
                    continue;
                }
                if (!downloadMap.ContainsKey(branch.branchName))
                {
                    downloadMap.Add(branch.branchName, downloadMap[branch.versionName]);
                    archiveTypeMap.Add(branch.branchName, archiveTypeMap[branch.versionName]);
                    resolvedHash.Add(branch.branchName, branch.versionName);
                }
            }

            // Resolve Git reference to Git commit hash.
            var gitCommit = resolvedHash.ContainsKey(request.GitRef) ? resolvedHash[request.GitRef] : request.GitRef;

            if (string.IsNullOrWhiteSpace(sourceUri))
            {
                // Normalize source URI value.
                sourceUri = null;
            }

            string fileUri, archiveType;
            if (!downloadMap.ContainsKey(gitCommit))
            {
                if (string.IsNullOrWhiteSpace(sourceUri))
                {
                    throw new InvalidOperationException("Unable to resolve binary package for version \"" +
                                                        request.GitRef + "\" and platform \"" + request.Platform +
                                                        "\" and this package does not have a source repository");
                }
                else
                {
                    RedirectableConsole.WriteLine("Unable to resolve binary package for version \"" + request.GitRef +
                                      "\" and platform \"" + request.Platform + "\", falling back to source version");
                    fileUri = null;
                    archiveType = null;
                }
            }
            else
            {
                fileUri = downloadMap[gitCommit];
                archiveType = archiveTypeMap[gitCommit];
            }

            return new ProtobuildPackageMetadata(
                request.Uri,
                type,
                sourceUri,
                request.Platform,
                gitCommit,
                archiveType,
                fileUri,
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
                },
                _binaryPackageResolve.GetProtobuildPackageBinary);
        }

        private dynamic GetJSON(Uri indexUri, out string str)
        {
            using (var client = new RetryableWebClient())
            {
                str = client.DownloadString(indexUri);
                return JSON.ToDynamic(str);
            }
        }

        private string GetLookupCacheFilename(string uri)
        {
            var sha1 = new SHA1Managed();
            var urlHashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(uri));
            var urlHashString = BitConverter.ToString(urlHashBytes).Replace("-", "").ToLowerInvariant();

            return Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                ".lookup-result." + urlHashString);
        }
    }
}
