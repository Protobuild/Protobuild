using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using fastJSON;

namespace Protobuild.Internal
{
    public class ProtobuildPackageProtocol : IPackageProtocol
    {
        private readonly IPackageCacheConfiguration _packageCacheConfiguration;

        public ProtobuildPackageProtocol(
            IPackageCacheConfiguration packageCacheConfiguration)
        {
            _packageCacheConfiguration = packageCacheConfiguration;
        }

        public string[] Schemes => new[] {"http", "https" };

        public IPackageMetadata ResolveSource(string uri, bool preferCacheLookup, string platform)
        {
            var baseUri = new Uri(uri);

            var apiUri = new Uri(baseUri.ToString().TrimEnd('/') + "/api");
            dynamic apiData = null;

            var performOnlineLookup = true;
            if (preferCacheLookup)
            {
                performOnlineLookup = false;
                if (File.Exists(this.GetLookupCacheFilename(uri)))
                {
                    try
                    {
                        using (var reader = new StreamReader(this.GetLookupCacheFilename(uri)))
                        {
                            apiData = JSON.ToDynamic(reader.ReadToEnd());
                        }
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
                        using (var writer = new StreamWriter(this.GetLookupCacheFilename(uri)))
                        {
                            writer.Write(jsonString);
                        }
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("WARNING: Unable to save cached result of request.");
                    }
                }
                catch (WebException)
                {
                    // Attempt to retrieve it from the lookup cache.
                    if (File.Exists(this.GetLookupCacheFilename(uri)))
                    {
                        var shouldThrow = false;
                        try
                        {
                            using (var reader = new StreamReader(this.GetLookupCacheFilename(uri)))
                            {
                                apiData = JSON.ToDynamic(reader.ReadToEnd());
                            }
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
                catch (InvalidOperationException)
                {
                    // Attempt to retrieve it from the lookup cache.
                    if (File.Exists(this.GetLookupCacheFilename(uri)))
                    {
                        var shouldThrow = false;
                        try
                        {
                            using (var reader = new StreamReader(this.GetLookupCacheFilename(uri)))
                            {
                                apiData = JSON.ToDynamic(reader.ReadToEnd());
                            }
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
                Console.WriteLine("WARNING: This package does not have a source repository set.");
            }

            var downloadMap = new Dictionary<string, string>();
            var archiveTypeMap = new Dictionary<string, string>();
            var resolvedHash = new Dictionary<string, string>();
            foreach (var ver in apiData.result.versions)
            {
                if (ver.platformName != platform)
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

            return new ProtobuildPackageMetadata
            {
                SourceURI = sourceUri,
                PackageType = type,
                DownloadMap = downloadMap,
                ArchiveTypeMap = archiveTypeMap,
                ResolvedHash = resolvedHash,
            };
        }

        private dynamic GetJSON(Uri indexUri, out string str)
        {
            try
            {
                using (var client = new WebClient())
                {
                    Console.WriteLine("HTTP GET " + indexUri);
                    str = client.DownloadString(indexUri);
                    return JSON.ToDynamic(str);
                }
            }
            catch (WebException)
            {
                Console.WriteLine("Web exception when retrieving: " + indexUri);
                throw;
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
