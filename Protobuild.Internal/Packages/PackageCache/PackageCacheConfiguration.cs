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

        public string GetRedirectsFile()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "protobuild-redirects.txt");
        }
    }
}

