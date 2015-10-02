using System.IO;
using System;
using System.Net;
using System.Collections.Generic;

namespace Protobuild
{
    public class PackageRetrieval : IPackageRetrieval
    {
        private readonly IPackageLookup _packageLookup;

        private readonly IProgressiveWebOperation _progressiveWebOperation;

        public PackageRetrieval(
            IPackageLookup packageLookup,
            IProgressiveWebOperation progressiveWebOperation)
        {
            _packageLookup = packageLookup;
            _progressiveWebOperation = progressiveWebOperation;
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

            var attempts = 10;
            while (attempts > 0)
            {
                try
                {
                    using (var stream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        stream.Write(packageData, 0, packageData.Length);
                    }
                    break;
                }
                catch (IOException ex)
                {
                    // On Windows, we can't write out the package file if another instance of Protobuild
                    // is writing it out at the moment.  Just wait and retry in another second.
                    Console.WriteLine("WARNING: Unable to write downloaded package file (attempt " + (11 - attempts) + " / 10)");
                    System.Threading.Thread.Sleep(5000);
                    attempts--;
                }
            }

            if (attempts == 0)
            {
                Console.WriteLine(
                    "WARNING: Unable to write out downloaded package!  Assuming " +
                    "another instance of Protobuild will provide it.");
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
            GitUtils.RunGit(targetPath, "git clone --progress --bare " + gitUrl + " .");
        }

        public byte[] DownloadBinaryPackage(string uri, string gitHash, string platform, out string format)
        {
            format = null;

            string sourceUri, type;
            Dictionary<string, string> downloadMap, archiveTypeMap, resolvedHash;
            IPackageTransformer transformer;
            _packageLookup.Lookup(
                uri,
                platform,
                true,
                out sourceUri, 
                out type,
                out downloadMap,
                out archiveTypeMap,
                out resolvedHash,
                out transformer);

            if (transformer != null)
            {
                return transformer.Transform(sourceUri, gitHash, platform, out format);
            }

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
            try
            {
                return _progressiveWebOperation.Get(fileUri);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("Unable to download binary package for version \"" + gitHash + "\" and platform \"" + platform + "\", falling back to source version");
                return null;
            }
        }
    }
}

