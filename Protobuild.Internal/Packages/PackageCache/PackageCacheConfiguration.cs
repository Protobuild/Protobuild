using System;
using System.IO;

namespace Protobuild
{
    internal class PackageCacheConfiguration : IPackageCacheConfiguration
    {
        public string GetCacheDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var cacheDirectory = Path.Combine(appData, "protobuild-cache-directory.txt");
            var defaultCacheDirectory = Path.Combine(appData, ".protobuild-cache");
            string directory;

            if (File.Exists(cacheDirectory))
            {
                using (var reader = new StreamReader(cacheDirectory))
                {
                    directory = reader.ReadToEnd().Trim();
                }
            }
            else
            {
                directory = defaultCacheDirectory;
            }

            try
            {
                Directory.CreateDirectory(directory);
            }
            catch
            {
                Console.WriteLine(
                    "WARNING: Cache directory '" + directory + "' specified in " + 
                    cacheDirectory + 
                    " could not be created.  Using default cache directory.");
                directory = defaultCacheDirectory;
            }

            // Inline check: If the cache directory is not the default cache directory, move
            // all files and folders from the default cache directory (if it exists) to the
            // cache directory.  This allows users to migrate existing cache data automatically.
            if (directory != defaultCacheDirectory && Directory.Exists(defaultCacheDirectory))
            {
                Console.WriteLine("New cache directory detected, migrating existing data...");
                var directoryInfo = new DirectoryInfo(defaultCacheDirectory);
                foreach (var file in directoryInfo.GetFiles())
                {
                    Console.WriteLine("Moving cache file " + file.Name + "...");
                    if (File.Exists(Path.Combine(directory, file.Name)))
                    {
                        File.Delete(Path.Combine(directory, file.Name));
                    }
                    file.MoveTo(Path.Combine(directory, file.Name));
                }
                foreach (var subdirectory in directoryInfo.GetDirectories())
                {
                    Console.WriteLine("Moving cache directory " + subdirectory.Name + "...");
                    if (Directory.Exists(Path.Combine(directory, subdirectory.Name)))
                    {
                        Directory.Delete(Path.Combine(directory, subdirectory.Name), true);
                    }
                    subdirectory.MoveTo(Path.Combine(directory, subdirectory.Name));
                }

                try
                {
                    Directory.Delete(defaultCacheDirectory);
                }
                catch
                {
                    Console.WriteLine("WARNING: Unable to remove default cache directory!");
                }
            }

            return directory;
        }

        public string GetRedirectsFile()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "protobuild-redirects.txt");
        }
    }
}

