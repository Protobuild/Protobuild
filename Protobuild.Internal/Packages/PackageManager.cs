using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using Protobuild.Tasks;
using fastJSON;

namespace Protobuild
{
    public class PackageManager
    {
        private readonly PackageCache m_PackageCache;

        public const string ARCHIVE_FORMAT_TAR_LZMA = "tar/lzma";

        public const string ARCHIVE_FORMAT_TAR_GZIP = "tar/gzip";

        public PackageManager()
        {
            this.m_PackageCache = new PackageCache();
        }

        public void ResolveAll(ModuleInfo module, string platform)
        {
            if (module.Packages == null || module.Packages.Count == 0)
            {
                return;
            }

            Console.WriteLine("Starting resolution of packages...");

            // TODO: Remove this notice when packaging is no longer experimental.
            Console.WriteLine(@"=========================== WARNING ===========================
Package management is currently an experimental feature.
Expect breaking changes and bugs to occur until this
functionality is stabilized.
=========================== WARNING ===========================");

            foreach (var submodule in module.Packages)
            {
                Console.WriteLine("Resolving: " + submodule.Uri);
                this.Resolve(submodule, platform, null);
            }

            Console.WriteLine("Package resolution complete.");
        }

        public void Resolve(PackageRef reference, string platform, bool? source)
        {
            var baseUri = reference.UriObject;

            var apiUri = new Uri(baseUri.ToString().TrimEnd('/') + "/api");
            var apiData = this.GetJSON(apiUri);

            if (apiData.has_error)
            {
                throw new InvalidOperationException((string)apiData.error);
            }

            var sourceUri = (string)apiData.result.package.gitUrl;

            if (!string.IsNullOrWhiteSpace(sourceUri))
            {
                try
                {
                    new Uri(sourceUri);
                }
                catch
                {
                    throw new InvalidOperationException(
                        "Received invalid Git URL when loading package from " + apiUri);
                }
            }
            else
            {
                Console.WriteLine("WARNING: This package does not have a source repository set.");
            }

            Directory.CreateDirectory(reference.Folder);

            if (source == null)
            {
                if (File.Exists(Path.Combine(reference.Folder, ".git")) || Directory.Exists(Path.Combine(reference.Folder, ".git")))
                {
                    Console.WriteLine("Git repository present at " + Path.Combine(reference.Folder, ".git") + "; leaving as source version.");
                    source = true;
                }
                else
                {
                    Console.WriteLine("Package type not specified (and no file at " + Path.Combine(reference.Folder, ".git") + "), requesting binary version.");
                    source = false;
                }
            }

            if (source.Value && !string.IsNullOrWhiteSpace(sourceUri))
            {
                this.ResolveSource(reference, sourceUri);
            }
            else
            {
                this.ResolveBinary(reference, platform, sourceUri, apiData);
            }
        }

        private void ResolveSource(PackageRef reference, string source)
        {
            if (File.Exists(Path.Combine(reference.Folder, ".git")) || Directory.Exists(Path.Combine(reference.Folder, ".git")))
            {
                Console.WriteLine("Git submodule / repository already present at " + reference.Folder);
                return;
            }

            this.EmptyReferenceFolder(reference.Folder);

            if (this.IsGitRepository())
            {
                this.UnmarkIgnored(reference.Folder);
                this.RunGit(null, "submodule update --init --recursive");

                if (!File.Exists(Path.Combine(reference.Folder, ".git")))
                {
                    // The submodule has never been added.
                    this.RunGit(null, "submodule add " + source + " " + reference.Folder);
                    this.RunGit(reference.Folder, "checkout -f " + reference.GitRef);
                    this.RunGit(null, "submodule update --init --recursive");
                    this.RunGit(null, "add .gitmodules");
                    this.RunGit(null, "add " + reference.Folder);
                }

                this.MarkIgnored(reference.Folder);
            }
            else
            {
                // The current folder isn't a Git repository, so use
                // git clone instead of git submodule.
                this.RunGit(null, "clone " + source + " " + reference.Folder);
                this.RunGit(reference.Folder, "checkout -f " + reference.GitRef);
            }
        }

        private void ResolveBinary(PackageRef reference, string platform, string source, dynamic apiData)
        {
            if (File.Exists(Path.Combine(reference.Folder, platform, ".pkg")))
            {
                Console.WriteLine("Protobuild binary package already present at " + Path.Combine(reference.Folder, platform));
                return;
            }

            var folder = Path.Combine(reference.Folder, platform);

            Console.WriteLine("Creating and emptying " + folder);

            if (File.Exists(Path.Combine(reference.Folder, ".pkg")))
            {
                if (Directory.Exists(folder))
                {
                    // Only clear out the target's folder if the reference folder
                    // already contains binary packages (for other platforms)
                    this.EmptyReferenceFolder(folder);
                }
            }
            else
            {
                // The reference folder is holding source code, so clear it
                // out entirely.
                this.EmptyReferenceFolder(reference.Folder);
            }

            Directory.CreateDirectory(folder);

            Console.WriteLine("Marking " + reference.Folder + " as ignored for Git");
            this.MarkIgnored(reference.Folder);

            var downloadMap = new Dictionary<string, string>();
            var archiveTypeMap = new Dictionary<string, string>();
            var resolvedHash = new Dictionary<string, string>();

            foreach (var ver in apiData.result.versions)
            {
                if (ver.platformName != platform)
                {
                    continue;
                }

                if (!downloadMap.ContainsKey(ver.versionName))
                {
                    downloadMap.Add(ver.versionName, ver.downloadUrl);
                    archiveTypeMap.Add(ver.versionName, ver.archiveType);
                    resolvedHash.Add(ver.versionName, ver.versionName);
                }
            }

            foreach (var branch in apiData.result.branches)
            {
                if (!downloadMap.ContainsKey(branch.versionName))
                {
                    continue;
                }

                if (!downloadMap.ContainsKey(branch.branchName))
                {
                    downloadMap.Add(branch.branchName, downloadMap[branch.versionName]);
                    archiveTypeMap.Add(branch.branchName, archiveTypeMap[branch.versionName]);
                    resolvedHash.Add(branch.branchName, branch.versionName);
                }
            }

            if (!downloadMap.ContainsKey(reference.GitRef))
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    throw new InvalidOperationException(
                        "Unable to resolve binary package for version \"" + reference.GitRef + 
                        "\" and platform \"" + platform + "\" and this package does not have a source repository");
                }
                else
                {
                    Console.WriteLine("Unable to resolve binary package for version \"" + reference.GitRef + 
                        "\" and platform \"" + platform + "\", falling back to source version");
                    this.ResolveSource(reference, source);
                    return;
                }
            }
                
            var uri = downloadMap[reference.GitRef];
            var archiveType = archiveTypeMap[reference.GitRef];
            var resolvedGitHash = resolvedHash[reference.GitRef];

            byte[] packageData;
            string format;
            if (this.m_PackageCache.HasPackage(uri, resolvedGitHash, platform, out format))
            {
                Console.WriteLine("Retrieving binary package from cache");
                packageData = this.m_PackageCache.GetPackage(uri, resolvedGitHash, platform);
            }
            else
            {
                Console.WriteLine("Retrieving binary package from " + uri);
                format = archiveType;
                packageData = this.GetBinary(uri);
                this.m_PackageCache.SavePackage(uri, resolvedGitHash, platform, format, packageData);
            }

            Console.WriteLine("Unpacking binary package from " + format + " archive");
            switch (format)
            {
                case ARCHIVE_FORMAT_TAR_GZIP:
                    {
                        using (var memory = new MemoryStream(packageData))
                        {
                            using (var decompress = new GZipStream(memory, CompressionMode.Decompress))
                            {
                                using (var memory2 = new MemoryStream())
                                {
                                    decompress.CopyTo(memory2);
                                    memory2.Seek(0, SeekOrigin.Begin);

                                    var reader = new tar_cs.TarReader(memory2);

                                    reader.ReadToEnd(folder);
                                }
                            }
                        }
                        break;
                    }
                case ARCHIVE_FORMAT_TAR_LZMA:
                    {
                        using (var inMemory = new MemoryStream(packageData))
                        {
                            using (var outMemory = new MemoryStream())
                            {
                                LZMA.LzmaHelper.Decompress(inMemory, outMemory);
                                outMemory.Seek(0, SeekOrigin.Begin);

                                var reader = new tar_cs.TarReader(outMemory);

                                reader.ReadToEnd(folder);
                            }
                        }
                        break;
                    }
                default:
                    throw new InvalidOperationException(
                        "This version of Protobuild does not support the " + 
                        format + " package format.");
            }

            // Only copy ourselves to the binary folder if both "Build/Module.xml" and
            // "Build/Projects" exist in the binary package's folder.  This prevents us
            // from triggering the "create new module?" logic if the package hasn't been
            // setup correctly.
            if (Directory.Exists(Path.Combine(folder, "Build", "Projects")) && 
                File.Exists(Path.Combine(folder, "Build", "Module.xml")))
            {
                var sourceProtobuild = Assembly.GetEntryAssembly().Location;
                File.Copy(sourceProtobuild, Path.Combine(folder, "Protobuild.exe"), true);
            }

            var file = File.Create(Path.Combine(folder, ".pkg"));
            file.Close();

            file = File.Create(Path.Combine(reference.Folder, ".pkg"));
            file.Close();

            Console.WriteLine("Binary resolution complete");
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
                            Console.Write("\rDownloading package; " + e.ProgressPercentage + "% complete (" + (e.BytesReceived / 1024) + "kb received)");
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

        private void UnmarkIgnored(string folder)
        {
            var excludePath = this.GetGitExcludePath(folder);

            if (excludePath == null)
            {
                return;
            }

            var contents = this.GetFileStringList(excludePath).ToList();
            contents.Remove(folder);
            this.SetFileStringList(excludePath, contents);
        }

        private void MarkIgnored(string folder)
        {
            var excludePath = this.GetGitExcludePath(folder);

            if (excludePath == null)
            {
                return;
            }

            var contents = this.GetFileStringList(excludePath).ToList();
            contents.Add(folder);
            this.SetFileStringList(excludePath, contents);
        }

        private string GetGitExcludePath(string folder)
        {
            var root = this.GetGitRootPath(folder);

            if (root == null)
            {
                return null;
            }
            else 
            {
                return Path.Combine(root, ".git", "info", "exclude");
            }
        }

        private string GetGitRootPath(string folder)
        {
            var current = folder;

            while (current != null && !Directory.Exists(Path.Combine(folder, ".git")))
            {
                var parent = new DirectoryInfo(current).Parent;

                if (parent == null)
                {
                    current = null;
                }
                else 
                {
                    current = parent.FullName;
                }
            }

            return current;
        }

        private void RunGit(string folder, string str)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = str,
                WorkingDirectory = folder == null ? Environment.CurrentDirectory : Path.Combine(Environment.CurrentDirectory, folder)
            };

            Console.WriteLine("Executing: git " + str);

            var process = Process.Start(processStartInfo);
            process.WaitForExit();
        }

        private bool IsGitRepository()
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "status",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            var process = Process.Start(processStartInfo);
            process.WaitForExit();

            if (process.ExitCode == 128)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void SetFileStringList(string excludePath, IEnumerable<string> contents)
        {
            using (var writer = new StreamWriter(excludePath, false))
            {
                foreach (var line in contents)
                {
                    writer.WriteLine(line);
                }
            }
        }

        private IEnumerable<string> GetFileStringList(string excludePath)
        {
            var results = new List<string>();

            using (var reader = new StreamReader(excludePath))
            {
                while (!reader.EndOfStream)
                {
                    results.Add(reader.ReadLine());
                }
            }

            return results;
        }

        private void EmptyReferenceFolder(string folder)
        {
            Directory.Delete(folder, true);
        }

        private dynamic GetJSON(Uri indexUri)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var str = client.DownloadString(indexUri);
                    return JSON.ToDynamic(str);
                }
            }
            catch (WebException)
            {
                Console.WriteLine("Web exception when retrieving: " + indexUri);
                throw;
            }
        }
    }
}

