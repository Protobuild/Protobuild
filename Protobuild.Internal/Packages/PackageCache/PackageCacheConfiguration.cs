using System;
using System.IO;

namespace Protobuild
{
    public class PackageCacheConfiguration : IPackageCacheConfiguration
    {
        public string GetCacheDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var directory = Path.Combine(appData, ".protobuild-cache");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}

