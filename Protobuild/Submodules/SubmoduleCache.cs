// ====================================================================== //
// This source code is licensed in accordance with the licensing outlined //
// on the main Tychaia website (www.tychaia.com).  Changes to the         //
// license on the website apply retroactively.                            //
// ====================================================================== //
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Protobuild.Submodules
{
    public class SubmoduleCache
    {
        private string GetCacheDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var directory = Path.Combine(appData, ".protobuild-cache");
            Directory.CreateDirectory(directory);
            return directory;
        }

        private string GetPackageName(string url, string gitHash, string platform)
        {
            var sha1 = new SHA1Managed();

            var urlHashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(url));
            var urlHashString = BitConverter.ToString(urlHashBytes).Replace("-", "").ToLowerInvariant();
            var gitHashString = gitHash.ToLowerInvariant();
            var platformString = platform.ToLowerInvariant();

            return urlHashString + "-" + gitHashString + "-" + platformString + ".tar.gz";
        }

        public bool HasPackage(string url, string gitHash, string platform)
        {
            var file = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform));
            return File.Exists(file);
        }

        public byte[] GetPackage(string url, string gitHash, string platform)
        {
            var file = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform));
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return data;
            }
        }

        public void SavePackage(string url, string gitHash, string platform, byte[] data)
        {
            var tempFile = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform) + ".tmp");
            using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(data, 0, data.Length);
            }

            var file = Path.Combine(
                this.GetCacheDirectory(),
                this.GetPackageName(url, gitHash, platform));
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

