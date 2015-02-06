using System.IO;
using System;
using System.Net;
using System.Collections.Generic;

namespace Protobuild
{
    public class PackageRetrieval : IPackageRetrieval
    {
        private IPackageLookup _packageLookup;

        public PackageRetrieval(IPackageLookup packageLookup)
        {
            _packageLookup = packageLookup;
        }

        public bool DownloadBinaryPackage(string uri, string gitHash, string platform, out string format, string targetPath)
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            var packageData = this.DownloadBinaryPackage(uri, gitHash, platform, out format);
            if (packageData == null)
            {
                // There is no binary package available.
                return false;
            }

            using (var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(packageData, 0, packageData.Length);
            }

            return true;
        }

        public void DownloadSourcePackage(string gitUrl, string targetPath)
        {
            if (Directory.Exists(targetPath))
            {
                throw new Exception("Unable to download source package; target path already exists!");
            }

            if (File.Exists(targetPath))
            {
                throw new Exception("Target path already exists (but is not a directory)");
            }

            Directory.CreateDirectory(targetPath);
            GitUtils.RunGit(targetPath, "git clone --bare " + gitUrl + " .");
        }

        private byte[] DownloadBinaryPackage(string uri, string gitHash, string platform, out string format)
        {
            format = null;

            string sourceUri, type;
            Dictionary<string, string> downloadMap, archiveTypeMap, resolvedHash;
            _packageLookup.Lookup(
                uri,
                platform,
                true,
                out sourceUri, 
                out type,
                out downloadMap,
                out archiveTypeMap,
                out resolvedHash);

            if (!downloadMap.ContainsKey(gitHash))
            {
                if (string.IsNullOrWhiteSpace(sourceUri))
                {
                    throw new InvalidOperationException("Unable to resolve binary package for version \"" + gitHash + "\" and platform \"" + platform + "\" and this package does not have a source repository");
                }
                else
                {
                    Console.WriteLine("Unable to resolve binary package for version \"" + gitHash + "\" and platform \"" + platform + "\", falling back to source version");
                    return null;
                }
            }
            var fileUri = downloadMap[gitHash];
            var archiveType = archiveTypeMap[gitHash];
            var resolvedGitHash = resolvedHash[gitHash];

            format = archiveType;
            return this.GetBinary(fileUri);
        }

        private byte[] GetBinary(string packageUri)
        {
            try 
            {
                using (var client = new WebClient())
                {
                    var done = false;
                    byte[] result = null;
                    Exception ex = null;
                    var downloadProgressRenderer = new DownloadProgressRenderer();
                    client.DownloadDataCompleted += (sender, e) => {
                        if (e.Error != null)
                        {
                            ex = e.Error;
                        }

                        result = e.Result;
                        done = true;
                    };
                    client.DownloadProgressChanged += (sender, e) => {
                        if (!done)
                        {
                            downloadProgressRenderer.Update(e.ProgressPercentage, e.BytesReceived / 1024);
                        }
                    };
                    client.DownloadDataAsync(new Uri(packageUri));
                    while (!done)
                    {
                        System.Threading.Thread.Sleep(0);
                    }

                    Console.WriteLine();

                    if (ex != null)
                    {
                        throw new InvalidOperationException("Download error", ex);
                    }

                    return result;
                }
            }
            catch (WebException)
            {
                Console.WriteLine("Web exception when retrieving: " + packageUri);
                throw;
            }
        }
    }
}

