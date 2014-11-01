using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Protobuild
{
    public class PackageCache
    {
        private string GetCacheDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var directory = Path.Combine(appData, ".protobuild-cache");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private string GetPackageName(string url, string gitHash, string platform, string format)
        {
            var sha1 = new SHA1Managed();

            var urlHashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(url));
            var urlHashString = BitConverter.ToString(urlHashBytes).Replace("-", "").ToLowerInvariant();
            var gitHashString = gitHash.ToLowerInvariant();
            var platformString = platform.ToLowerInvariant();

            return urlHashString + "-" + gitHashString + "-" + platformString + "." + TranslateToExtension(format);
        }

        private string TranslateToExtension(string format)
        {
            switch (format)
            {
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    return "tar.lzma";
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                    return "tar.gz";
                default:
                    throw new InvalidOperationException("Archive format not supported in cache.");
            }
        }

        public bool HasPackage(string url, string gitHash, string platform, out string format)
        {
            format = null;
            var lzmaFile = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, PackageManager.ARCHIVE_FORMAT_TAR_LZMA));
            if (File.Exists(lzmaFile)) 
            {
                format = PackageManager.ARCHIVE_FORMAT_TAR_LZMA;
                return true;
            }

            var gzFile = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, PackageManager.ARCHIVE_FORMAT_TAR_GZIP));
            if (File.Exists(gzFile)) 
            {
                format = PackageManager.ARCHIVE_FORMAT_TAR_GZIP;
                return true;
            }

            return false;
        }

        public byte[] GetPackage(string url, string gitHash, string platform)
        {
            string format;
            this.HasPackage(url, gitHash, platform, out format);
            var file = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, format));
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return data;
            }
        }

        public void SavePackage(string url, string gitHash, string platform, string format, byte[] data)
        {
            var tempFile = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, format) + ".tmp");
            using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(data, 0, data.Length);
            }

            var file = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform, format));
            try
            {
                File.Move(tempFile, file);
            }
            catch (Exception)
            {
                Console.WriteLine("WARNING: Unable to save package to cache.");
            }
        }
    }
}

