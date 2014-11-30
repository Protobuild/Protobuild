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

        public const string PACKAGE_TYPE_LIBRARY = "library";

        public const string PACKAGE_TYPE_TEMPLATE = "template";

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

            foreach (var submodule in module.Packages)
            {
                Console.WriteLine("Resolving: " + submodule.Uri);
                this.Resolve(submodule, platform, null, null);
            }

            Console.WriteLine("Package resolution complete.");
        }

        public void Resolve(PackageRef reference, string platform, string templateName, bool? source)
        {
            var baseUri = reference.UriObject;

            var apiUri = new Uri(baseUri.ToString().TrimEnd('/') + "/api");
            var apiData = this.GetJSON(apiUri);

            if (apiData.has_error)
            {
                throw new InvalidOperationException((string)apiData.error);
            }

            var sourceUri = (string)apiData.result.package.gitUrl;
            var type = (string)apiData.result.package.type;

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

            if (type == PackageManager.PACKAGE_TYPE_TEMPLATE && templateName == null)
            {
                throw new InvalidOperationException(
                    "Template referenced as part of module packages.  Templates can only be used " +
                    "with the --start option.");
            }
            else if (type == PackageManager.PACKAGE_TYPE_LIBRARY)
            {
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
            }

            if (source.Value && !string.IsNullOrWhiteSpace(sourceUri))
            {
                switch (type)
                {
                    case PackageManager.PACKAGE_TYPE_LIBRARY:
                        this.ResolveLibrarySource(reference, sourceUri);
                        break;
                    case PackageManager.PACKAGE_TYPE_TEMPLATE:
                        this.ResolveTemplateSource(reference, templateName, sourceUri);
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case PackageManager.PACKAGE_TYPE_LIBRARY:
                        this.ResolveLibraryBinary(reference, platform, sourceUri, apiData);
                        break;
                    case PackageManager.PACKAGE_TYPE_TEMPLATE:
                        this.ResolveTemplateBinary(reference, templateName, platform, sourceUri, apiData);
                        break;
                }
            }
        }

        private void ResolveLibrarySource(PackageRef reference, string source)
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
                this.RunGit(reference.Folder, "submodule update --init --recursive");
            }
        }

        private void ResolveTemplateSource(PackageRef reference, string templateName, string source)
        {
            if (reference.Folder != string.Empty)
            {
                throw new InvalidOperationException("Reference folder must be empty for template type.");
            }

            if (Directory.Exists(".staging"))
            {
                Directory.Delete(".staging", true);
            }

            this.RunGit(null, "clone " + source + " .staging");
            this.RunGit(".staging", "checkout -f " + reference.GitRef);

            this.ApplyProjectTemplateFromStaging(templateName);
        }

        private void ResolveLibraryBinary(PackageRef reference, string platform, string source, dynamic apiData)
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

            string format;
            var packageData = DownloadBinaryPackage(reference, platform, source, apiData, out format);
            if (packageData == null)
            {
                this.ResolveLibrarySource(reference, source);
                return;
            }

            ExtractPackageToFolder(folder, format, packageData);

            // Only copy ourselves to the binary folder if both "Build/Module.xml" and
            // "Build/Projects" exist in the binary package's folder.  This prevents us
            // from triggering the "create new module?" logic if the package hasn't been
            // setup correctly.
            if (Directory.Exists(Path.Combine(folder, "Build", "Projects")) && 
                File.Exists(Path.Combine(folder, "Build", "Module.xml")))
            {
                var sourceProtobuild = Assembly.GetEntryAssembly().Location;
                File.Copy(sourceProtobuild, Path.Combine(folder, "Protobuild.exe"), true);

                try
                {
                    var chmodStartInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = "a+x Protobuild.exe",
                        WorkingDirectory = folder,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(chmodStartInfo);
                }
                catch
                {
                }
            }

            var file = File.Create(Path.Combine(folder, ".pkg"));
            file.Close();

            file = File.Create(Path.Combine(reference.Folder, ".pkg"));
            file.Close();

            Console.WriteLine("Binary resolution complete");
        }

        private void ResolveTemplateBinary(PackageRef reference, string templateName, string platform, string sourceUri, dynamic apiData)
        {
            if (reference.Folder != string.Empty)
            {
                throw new InvalidOperationException("Reference folder must be empty for template type.");
            }

            if (Directory.Exists(".staging"))
            {
                Directory.Delete(".staging", true);
            }

            Directory.CreateDirectory(".staging");

            string format;
            var packageData = DownloadBinaryPackage(reference, platform, sourceUri, apiData, out format);
            if (packageData == null)
            {
                this.ResolveTemplateSource(reference, templateName, sourceUri);
                return;
            }

            ExtractPackageToFolder(".staging", format, packageData);

            ApplyProjectTemplateFromStaging(templateName);
        }

        private void ApplyProjectTemplateFromStaging(string name)
        {
            foreach (var pathToFile in GetFilesFromStaging())
            {
                var path = pathToFile.Key;
                var file = pathToFile.Value;

                var replacedPath = path.Replace("{PROJECT_NAME}", name);
                var dirSeperator = replacedPath.LastIndexOfAny(new[] { '/', '\\' });
                if (dirSeperator != -1)
                {
                    var replacedDir = replacedPath.Substring(0, dirSeperator);
                    if (!Directory.Exists(replacedDir))
                    {
                        Directory.CreateDirectory(replacedDir);
                    }
                }

                string contents;
                using (var reader = new StreamReader(file.FullName))
                {
                    contents = reader.ReadToEnd();
                }

                if (contents.Contains("{PROJECT_NAME}") || contents.Contains("{PROJECT_XML_NAME}"))
                {
                    contents = contents.Replace("{PROJECT_NAME}", name);
                    contents = contents.Replace("{PROJECT_XML_NAME}", System.Security.SecurityElement.Escape(name));
                    using (var writer = new StreamWriter(replacedPath))
                    {
                        writer.Write(contents);
                    }
                }
                else
                {
                    // If we don't see {PROJECT_NAME} or {PROJECT_XML_NAME}, use a straight
                    // file copy so that we don't break binary files.
                    File.Copy(file.FullName, replacedPath, true);
                }
            }

            Directory.Delete(".staging", true);
        }

        private IEnumerable<KeyValuePair<string, FileInfo>> GetFilesFromStaging(string currentDirectory = null, string currentPrefix = null)
        {
            if (currentDirectory == null)
            {
                currentDirectory = ".staging";
                currentPrefix = string.Empty;
            }

            var dirInfo = new DirectoryInfo(currentDirectory);
            foreach (var subdir in dirInfo.GetDirectories("*"))
            {
                if (subdir.Name == ".git")
                {
                    continue;
                }

                var nextDirectory = Path.Combine(currentDirectory, subdir.Name);
                var nextPrefix = currentPrefix == string.Empty ? subdir.Name : Path.Combine(currentPrefix, subdir.Name);

                foreach (var kv in this.GetFilesFromStaging(nextDirectory, nextPrefix))
                {
                    yield return kv;
                }
            }

            foreach (var file in dirInfo.GetFiles("*"))
            {
                yield return new KeyValuePair<string, FileInfo>(Path.Combine(currentPrefix, file.Name), file);
            }
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

        private byte[] DownloadBinaryPackage(PackageRef reference, string platform, string source, dynamic apiData, out string format)
        {
            format = null;

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
                    throw new InvalidOperationException("Unable to resolve binary package for version \"" + reference.GitRef + "\" and platform \"" + platform + "\" and this package does not have a source repository");
                }
                else
                {
                    Console.WriteLine("Unable to resolve binary package for version \"" + reference.GitRef + "\" and platform \"" + platform + "\", falling back to source version");
                    return null;
                }
            }
            var uri = downloadMap[reference.GitRef];
            var archiveType = archiveTypeMap[reference.GitRef];
            var resolvedGitHash = resolvedHash[reference.GitRef];
            byte[] packageData;
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
            return packageData;
        }

        static void ExtractPackageToFolder(string folder, string format, dynamic packageData)
        {
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
                                var reduplicator = new Reduplicator();
                                reduplicator.UnpackTarToFolder(reader, folder);
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
                            var reduplicator = new Reduplicator();
                            reduplicator.UnpackTarToFolder(reader, folder);
                        }
                    }
                    break;
                }
                default:
                    throw new InvalidOperationException(
                        "This version of Protobuild does not support the " + 
                        format + " package format.");
            }
        }
    }
}

