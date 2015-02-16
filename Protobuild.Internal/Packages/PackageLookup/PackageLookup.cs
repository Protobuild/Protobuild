using System;
using System.Net;
using fastJSON;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Protobuild
{
    public class PackageLookup : IPackageLookup
    {
        private IPackageCacheConfiguration _packageCacheConfiguration;
        private IPackageRedirector _packageRedirector;

        public PackageLookup(
            IPackageCacheConfiguration packageCacheConfiguration,
            IPackageRedirector packageRedirector)
        {
            _packageCacheConfiguration = packageCacheConfiguration;
            _packageRedirector = packageRedirector;
        }

        public void Lookup(
            string uri,
            string platform,
            bool preferCacheLookup,
            out string sourceUri, 
            out string type,
            out Dictionary<string, string> downloadMap,
            out Dictionary<string, string> archiveTypeMap,
            out Dictionary<string, string> resolvedHash)
        {
            uri = _packageRedirector.RedirectPackageUrl(uri);

            if (uri.StartsWith("local-git://", StringComparison.InvariantCultureIgnoreCase))
            {
                sourceUri = uri.Substring("local-git://".Length);
                type = PackageManager.PACKAGE_TYPE_LIBRARY;
                downloadMap = new Dictionary<string, string>();
                archiveTypeMap = new Dictionary<string, string>();
                resolvedHash = new Dictionary<string, string>();
                return;
            }

            if (uri.StartsWith("http-git://", StringComparison.InvariantCultureIgnoreCase))
            {
                sourceUri = "http://" + uri.Substring("http-git://".Length);
                type = PackageManager.PACKAGE_TYPE_LIBRARY;
                downloadMap = new Dictionary<string, string>();
                archiveTypeMap = new Dictionary<string, string>();
                resolvedHash = new Dictionary<string, string>();
                return;
            }

            if (uri.StartsWith("https-git://", StringComparison.InvariantCultureIgnoreCase))
            {
                sourceUri = "https://" + uri.Substring("https-git://".Length);
                type = PackageManager.PACKAGE_TYPE_LIBRARY;
                downloadMap = new Dictionary<string, string>();
                archiveTypeMap = new Dictionary<string, string>();
                resolvedHash = new Dictionary<string, string>();
                return;
            }

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
                    using (var writer = new StreamWriter(this.GetLookupCacheFilename(uri)))
                    {
                        writer.Write(jsonString);
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

            sourceUri = (string)apiData.result.package.gitUrl;
            type = (string)apiData.result.package.type;

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

            downloadMap = new Dictionary<string, string>();
            archiveTypeMap = new Dictionary<string, string>();
            resolvedHash = new Dictionary<string, string>();
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
        }

        private dynamic GetJSON(Uri indexUri, out string str)
        {
            try
            {
                using (var client = new WebClient())
                {
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

