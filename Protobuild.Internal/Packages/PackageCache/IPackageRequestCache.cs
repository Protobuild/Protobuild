using System;

namespace Protobuild
{
    public interface IPackageRequestCache
    {
        bool IsCached(string id);

        string GetCachedData(string id);

        dynamic GetCachedJsonObject(string id);

        void StoreCachedData(string id, string data);

        string TryGetCachedData(string id, Func<string, string> getUncachedData);

        dynamic TryGetCachedJsonObject(string id, out string json, Func<string, string> getUncachedData);

        string TryGetOptionallyCachedData(string id, bool preferCache, Func<string, string> getUncachedData);

        dynamic TryGetOptionallyCachedJsonObject(string id, bool preferCache, out string json, Func<string, string> getUncachedData);
    }
}
