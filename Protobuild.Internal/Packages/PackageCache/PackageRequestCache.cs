using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using fastJSON;

namespace Protobuild
{
    internal class PackageRequestCache : IPackageRequestCache
    {
        private readonly IPackageCacheConfiguration _packageCacheConfiguration;

        public PackageRequestCache(IPackageCacheConfiguration packageCacheConfiguration)
        {
            _packageCacheConfiguration = packageCacheConfiguration;
        }

        public bool IsCached(string id)
        {
            return File.Exists(this.GetLookupCacheFilename(id));
        }

        public string GetCachedData(string id)
        {
            using (var reader = new StreamReader(this.GetLookupCacheFilename(id)))
            {
                var result = reader.ReadToEnd();
                RedirectableConsole.WriteLine("(from cache) " + id);
                return result;
            }
        }

        public dynamic GetCachedJsonObject(string id)
        {
            return JSON.ToDynamic(GetCachedData(id));
        }

        public void StoreCachedData(string id, string data)
        {
            using (var writer = new StreamWriter(this.GetLookupCacheFilename(id)))
            {
                writer.Write(data);
            }
        }

        public string TryGetCachedData(string id, Func<string, string> getUncachedData)
        {
            if (IsCached(id))
            {
                try
                {
                    return GetCachedData(id);
                }
                catch (ExecEnvironment.SelfInvokeExitException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Fallback to getting data uncached.
                }
            }

            var data = getUncachedData(id);

            try
            {
                StoreCachedData(id, data);
            }
            catch (ExecEnvironment.SelfInvokeExitException)
            {
                throw;
            }
            catch (Exception)
            {
                // Ignore as multiple processes might be trying to write
                // to the cache at the same time.
            }

            return data;
        }

        public dynamic TryGetCachedJsonObject(string id, out string json, Func<string, string> getUncachedData)
        {
            json = TryGetCachedData(id, getUncachedData);
            return JSON.ToDynamic(json);
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

        public string TryGetOptionallyCachedData(string id, bool preferCache, Func<string, string> getUncachedData)
        {
            Exception initialException = null;

            if (!preferCache)
            {
                // Always attempt to get uncached data first (prefer live version).
                try
                {
                    var data = getUncachedData(id);

                    // Try to store the result in the cache.
                    try
                    {
                        StoreCachedData(id, data);
                    }
                    catch (ExecEnvironment.SelfInvokeExitException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        // Ignore as multiple processes might be trying to write
                        // to the cache at the same time.
                    }

                    return data;
                }
                catch (ExecEnvironment.SelfInvokeExitException)
                {
                    throw;
                }
                catch (Exception)
                {
                    if (IsCached(id))
                    {
                        try
                        {
                            return GetCachedData(id);
                        }
                        catch (ExecEnvironment.SelfInvokeExitException)
                        {
                            throw;
                        }
                        catch (Exception)
                        {
                            // Let the original exception from the get uncached data lambda
                            // be thrown below.
                        }
                    }

                    throw;
                }
            }

            return TryGetCachedData(id, getUncachedData);
        }

        public dynamic TryGetOptionallyCachedJsonObject(string id, bool preferCache, out string json, Func<string, string> getUncachedData)
        {
            json = TryGetOptionallyCachedData(id, preferCache, getUncachedData);
            return JSON.ToDynamic(json);
        }
    }
}