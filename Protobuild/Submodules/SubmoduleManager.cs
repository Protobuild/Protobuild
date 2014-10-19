// ====================================================================== //
// This source code is licensed in accordance with the licensing outlined //
// on the main Tychaia website (www.tychaia.com).  Changes to the         //
// license on the website apply retroactively.                            //
// ====================================================================== //
using System;
using Protobuild.Tasks;
using System.Net;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;
using System.Diagnostics;

namespace Protobuild.Submodules
{
    public class SubmoduleManager
    {
        public void ResolveAll(ModuleInfo module, string platform, bool source)
        {
            if (module.Submodules == null || module.Submodules.Count == 0)
            {
                return;
            }

            Console.WriteLine("Starting resolution of submodules...");

            // TODO: Remove this notice when submodule and packaging is no
            // longer experimental.
            Console.WriteLine(@"=========================== WARNING ===========================
Resolvable submodules (aka. package management) is currently
an experimental feature.  Expect breaking changes and bugs to 
occur until this functionality is stabilized.
=========================== WARNING ===========================");

            foreach (var submodule in module.Submodules)
            {
                Console.WriteLine("Resolving: " + submodule.Uri);
                this.Resolve(submodule, platform, source);
            }

            Console.WriteLine("Submodule resolution complete.");
        }

        public void Resolve(SubmoduleRef reference, string platform, bool source)
        {
            var baseUri = reference.UriObject;

            var indexUri = new Uri(baseUri + "/index");
            var indexData = this.GetStringList(indexUri);

            if (indexData.Length == 0)
            {
                throw new InvalidOperationException(
                    "The specified submodule reference is not valid.");
            }

            var sourceUri = indexData[0];

            if (string.IsNullOrWhiteSpace(sourceUri))
            {
                try
                {
                    new Uri(sourceUri);
                }
                catch
                {
                    throw new InvalidOperationException(
                        "Received invalid Git URL when loading package from " + indexUri);
                }
            }
            else
            {
                Console.WriteLine("WARNING: This package does not have a source repository set.");
            }

            Directory.CreateDirectory(reference.Folder);

            if (source && !string.IsNullOrWhiteSpace(sourceUri))
            {
                this.ResolveSource(reference, sourceUri);
            }
            else
            {
                this.ResolveBinary(reference, platform, sourceUri, indexData);
            }
        }

        private void ResolveSource(SubmoduleRef reference, string source)
        {
            if (File.Exists(Path.Combine(reference.Folder, ".git")))
            {
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

        private void ResolveBinary(SubmoduleRef reference, string platform, string source, string[] indexData)
        {
            if (File.Exists(Path.Combine(reference.Folder, platform, ".pkg")))
            {
                return;
            }

            var folder = Path.Combine(reference.Folder, platform);

            Console.WriteLine("Creating and emptying " + folder);
            this.EmptyReferenceFolder(reference.Folder);
            Directory.CreateDirectory(folder);

            Console.WriteLine("Marking " + reference.Folder + " as ignored for Git");
            this.MarkIgnored(reference.Folder);

            var availableRefs = indexData.ToList();
            availableRefs.RemoveAt(0);

            if (!availableRefs.Contains(reference.GitRef))
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    throw new InvalidOperationException(
                        "Unable to resolve binary package for version \"" + reference.GitRef + 
                        "\" and this package does not have a source repository");
                }
                else
                {
                    Console.WriteLine("Unable to resolve binary package for version \"" + reference.GitRef + "\", falling back to source version");
                    this.ResolveSource(reference, source);
                    return;
                }
            }

            var baseUri = reference.UriObject;

            var platformsUri = new Uri(baseUri + "/" + reference.GitRef + "/platforms");

            Console.WriteLine("Checking for supported platforms at " + platformsUri);
            var platforms = this.GetStringList(platformsUri);

            var platformName = platform;

            if (!platforms.Contains(platformName))
            {
                if (string.IsNullOrWhiteSpace(source))
                {
                    throw new InvalidOperationException(
                        "Unable to resolve binary package for platform \"" + platformName + 
                        "\" and this package does not have a source repository");
                }
                else
                {
                    Console.WriteLine("Unable to resolve binary package for platform \"" + platformName + "\", falling back to source version");
                    this.ResolveSource(reference, source);
                    return;
                }
            }

            var uri = baseUri + "/" + reference.GitRef + "/" + platformName + ".tar.gz";
            Console.WriteLine("Retrieving binary package from " + uri);
            var packageData = this.GetBinary(uri);

            using (var memory = new MemoryStream(packageData))
            {
                using (var decompress = new GZipStream(memory, CompressionMode.Decompress))
                {
                    using (var memory2 = new MemoryStream())
                    {
                        decompress.CopyTo(memory2);
                        memory2.Seek(0, SeekOrigin.Begin);

                        var reader = new tar_cs.TarReader(memory2);

                        Console.WriteLine("Unpacking binary package");
                        reader.ReadToEnd(folder);
                    }
                }
            }

            // Only copy ourselves to the binary folder if both "Build/Module.xml" and
            // "Build/Projects" exist in the binary package's folder.  This prevents us
            // from triggering the "create new module?" logic if the package hasn't been
            // setup correctly.
            if (Directory.Exists(Path.Combine(folder, "Build", "Projects")) && 
                File.Exists(Path.Combine(folder, "Build", "Module.xml")))
            {
                var sourceProtobuild = typeof(SubmoduleManager).Assembly.Location;
                File.Copy(sourceProtobuild, Path.Combine(folder, "Protobuild.exe"), true);
            }

            var file = File.Create(Path.Combine(folder, ".pkg"));
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

        private string[] GetStringList(Uri indexUri)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var str = client.DownloadString(indexUri);
                    return str.Split(
                        new char[] { '\r', '\n' },
                        StringSplitOptions.RemoveEmptyEntries);
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

