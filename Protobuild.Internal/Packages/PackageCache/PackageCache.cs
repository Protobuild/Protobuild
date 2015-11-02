using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Protobuild
{
    public class PackageCache : IPackageCache
    {
        private IPackageRetrieval _packageRetrieval;
        private IPackageCacheConfiguration _packageCacheConfiguration;

        public PackageCache(
            IPackageRetrieval packageRetrieval,
            IPackageCacheConfiguration packageCacheConfiguration)
        {
            _packageRetrieval = packageRetrieval;
            _packageCacheConfiguration = packageCacheConfiguration;
        }

        public bool HasBinaryPackage(string url, string gitHash, string platform, out string format)
        {
            format = null;
            var lzmaFile = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, PackageManager.ARCHIVE_FORMAT_TAR_LZMA));
            if (File.Exists(lzmaFile)) 
            {
                format = PackageManager.ARCHIVE_FORMAT_TAR_LZMA;
                return true;
            }

            var gzFile = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, PackageManager.ARCHIVE_FORMAT_TAR_GZIP));
            if (File.Exists(gzFile)) 
            {
                format = PackageManager.ARCHIVE_FORMAT_TAR_GZIP;
                return true;
            }

            return false;
        }

        public bool HasSourcePackage(string url, string gitHash)
        {
            var sourceName = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(url, string.Empty, "Source", string.Empty));
            if (Directory.Exists(sourceName)) 
            {
                return true;
            }

            return false;
        }

        public IPackageContent GetBinaryPackage(string url, string gitHash, string platform)
        {
            string format;
            if (this.HasBinaryPackage(url, gitHash, platform, out format))
            {
                // We have it already downloaded in the cache.
                var file = Path.Combine(
                    _packageCacheConfiguration.GetCacheDirectory(),
                    this.GetPackageName(url, gitHash, platform, format));
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);
                    return new BinaryPackageContent
                    {
                        Format = format,
                        PackageData = data,
                    };
                }
            }

            // We must use the package retrieval interface to download a copy
            // of the package.
            var tempFile = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, string.Empty) + ".tmp");
            if (!_packageRetrieval.DownloadBinaryPackage(url, gitHash, platform, out format, tempFile))
            {
                return null;
            }

            var saveFile = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, format));
            try
            {
                File.Move(tempFile, saveFile);
            }
            catch (Exception)
            {
                Console.WriteLine("WARNING: Unable to save package to cache.");
                saveFile = tempFile;
            }

            byte[] saveData;
            using (var stream = new FileStream(saveFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                saveData = new byte[stream.Length];
                stream.Read(saveData, 0, saveData.Length);
            }

            if (saveFile == tempFile)
            {
                File.Delete(tempFile);
            }

            return new BinaryPackageContent
            {
                Format = format,
                PackageData = saveData,
            };
        }

        public IPackageContent GetSourcePackage(string url, string gitHash)
        {
            var sourcePath = Path.Combine(
                _packageCacheConfiguration.GetCacheDirectory(),
                this.GetPackageName(url, string.Empty, "Source", string.Empty));

            if (this.HasSourcePackage(url, gitHash))
            {
                if (Directory.Exists(Path.Combine(sourcePath, ".git")))
                {
                    try
                    {
                        GitUtils.RunGitAbsolute(sourcePath, "fetch origin +refs/heads/*:refs/heads/*");
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore exceptions here in case the user is offline.
                    }

                    return new SourcePackageContent(this)
                    {
                        SourcePath = sourcePath,
                        GitRef = gitHash,
                        OriginalGitUri = url,
                    };
                }
                else
                {
                    try
                    {
                        Directory.Delete(sourcePath, true);
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine("WARNING: Unable to delete invalid source package from cache!");
                    }
                }
            }

            Directory.CreateDirectory(sourcePath);
            GitUtils.RunGit(null, "clone --progress --bare " + url + " " + sourcePath);

            return new SourcePackageContent(this)
            {
                SourcePath = sourcePath,
                GitRef = gitHash,
                OriginalGitUri = url,
            };
        }

        private string GetPackageName(string url, string gitHash, string platform, string format)
        {
            var sha1 = new SHA1Managed();

            var urlHashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(url));
            var urlHashString = BitConverter.ToString(urlHashBytes).Replace("-", "").ToLowerInvariant();
            var gitHashString = gitHash.ToLowerInvariant();
            var platformString = platform.ToLowerInvariant();

            return urlHashString + "-" + gitHashString + "-" + platformString + TranslateToExtension(format);
        }

        private string TranslateToExtension(string format)
        {
            switch (format)
            {
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    return ".tar.lzma";
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                    return ".tar.gz";
                case "":
                    return string.Empty;
                default:
                    throw new InvalidOperationException("Archive format not supported in cache.");
            }
        }
    }
}

