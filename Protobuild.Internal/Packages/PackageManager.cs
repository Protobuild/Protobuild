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
    public class PackageManager : IPackageManager
    {
        private readonly IPackageCache m_PackageCache;

        private readonly IPackageLookup _packageLookup;

        private readonly IPackageLocator m_PackageLocator;

        private readonly IPackageGlobalTool m_PackageGlobalTool;

        private readonly IPackageRedirector packageRedirector;

        public const string ARCHIVE_FORMAT_TAR_LZMA = "tar/lzma";

        public const string ARCHIVE_FORMAT_TAR_GZIP = "tar/gzip";

        public const string PACKAGE_TYPE_LIBRARY = "library";

        public const string PACKAGE_TYPE_TEMPLATE = "template";

        public const string PACKAGE_TYPE_GLOBAL_TOOL = "global-tool";

        public PackageManager(
            IPackageCache packageCache,
            IPackageLookup packageLookup,
            IPackageLocator packageLocator,
            IPackageGlobalTool packageGlobalTool,
            IPackageRedirector packageRedirector)
        {
            this.packageRedirector = packageRedirector;
            this.m_PackageCache = packageCache;
            _packageLookup = packageLookup;
            this.m_PackageLocator = packageLocator;
            this.m_PackageGlobalTool = packageGlobalTool;
        }

        public void ResolveAll(ModuleInfo module, string platform)
        {
            Console.WriteLine("Starting resolution of packages...");

            if (module.Packages != null && module.Packages.Count > 0)
            {
                foreach (var submodule in module.Packages)
                {
                    Console.WriteLine("Resolving: " + submodule.Uri);
                    this.Resolve(module, submodule, platform, null, null);
                }
            }

            foreach (var submodule in module.GetSubmodules(platform))
            {
                Console.WriteLine(
                    "Invoking package resolution in submodule for " + submodule.Name);
                submodule.RunProtobuild("-resolve " + platform + " " + packageRedirector.GetRedirectionArguments());
                Console.WriteLine(
                    "Finished submodule package resolution for " + submodule.Name);
            }

            Console.WriteLine("Package resolution complete.");
        }

        public void Resolve(ModuleInfo module, PackageRef reference, string platform, string templateName, bool? source, bool forceUpgrade = false)
        {
            if (module != null && reference.Folder != null)
            {
                var existingPath = this.m_PackageLocator.DiscoverExistingPackagePath(module.Path, reference, platform);
                if (existingPath != null)
                {
                    Console.WriteLine("Found an existing working copy of this package at " + existingPath);

                    Directory.CreateDirectory(reference.Folder);
                    using (var writer = new StreamWriter(Path.Combine(reference.Folder, ".redirect")))
                    {
                        writer.WriteLine(existingPath);
                    }

                    return;
                }
                else
                {
                    if (File.Exists(Path.Combine(reference.Folder, ".redirect")))
                    {
                        try
                        {
                            File.Delete(Path.Combine(reference.Folder, ".redirect"));
                        }
                        catch
                        {
                        }
                    }
                }
            }

            if (reference.Folder == null) 
            {
                Console.WriteLine("Resolving package with null reference folder; this package must be a global tool.");
            }

            string sourceUri, type;
            Dictionary<string, string> downloadMap, archiveTypeMap, resolvedHash;
            IPackageTransformer transformer;
            _packageLookup.Lookup(
                reference.Uri,
                platform,
                !forceUpgrade && reference.IsCommitReference,
                out sourceUri, 
                out type,
                out downloadMap,
                out archiveTypeMap,
                out resolvedHash,
                out transformer);

            // Resolve Git reference to Git commit hash.
            var refUri = reference.Uri;
            var refFolder = reference.Folder;
            var gitCommit = resolvedHash.ContainsKey(reference.GitRef) ? resolvedHash[reference.GitRef] : reference.GitRef;
            reference = new PackageRef
            {
                Uri = refUri,
                Folder = refFolder,
                GitRef = gitCommit
            };

            string toolFolder = null;
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
            else if (type == PackageManager.PACKAGE_TYPE_GLOBAL_TOOL)
            {
                toolFolder = this.m_PackageGlobalTool.GetGlobalToolInstallationPath(reference);
                source = false;
            }

            if (source.Value && !string.IsNullOrWhiteSpace(sourceUri))
            {
                switch (type)
                {
                    case PackageManager.PACKAGE_TYPE_LIBRARY:
                        this.ResolveLibrarySource(reference, sourceUri, forceUpgrade);
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
                        this.ResolveLibraryBinary(reference, platform, sourceUri, forceUpgrade);
                        break;
                    case PackageManager.PACKAGE_TYPE_TEMPLATE:
                        this.ResolveTemplateBinary(reference, templateName, platform, sourceUri);
                        break;
                    case PackageManager.PACKAGE_TYPE_GLOBAL_TOOL:
                        this.ResolveGlobalToolBinary(reference, toolFolder, platform, forceUpgrade);
                        break;
                }
            }
        }

        private void ResolveLibrarySource(PackageRef reference, string source, bool forceUpgrade)
        {
            if (File.Exists(Path.Combine(reference.Folder, ".git")) || Directory.Exists(Path.Combine(reference.Folder, ".git")))
            {
                if (!forceUpgrade)
                {
                    Console.WriteLine("Git submodule / repository already present at " + reference.Folder);
                    return;
                }
            }

            this.EmptyReferenceFolder(reference.Folder);

            var package = m_PackageCache.GetSourcePackage(source, reference.GitRef);
            package.ExtractTo(reference.Folder);
        }

        private void ResolveTemplateSource(PackageRef reference, string templateName, string source)
        {
            if (reference.Folder != string.Empty)
            {
                throw new InvalidOperationException("Reference folder must be empty for template type.");
            }

            if (Directory.Exists(".staging"))
            {
                PathUtils.AggressiveDirectoryDelete(".staging");
            }

            var package = m_PackageCache.GetSourcePackage(source, reference.GitRef);
            package.ExtractTo(".staging");

            this.ApplyProjectTemplateFromStaging(templateName);
        }

        private void ResolveLibraryBinary(PackageRef reference, string platform, string source, bool forceUpgrade)
        {
            if (File.Exists(Path.Combine(reference.Folder, platform, ".pkg")))
            {
                if (!forceUpgrade)
                {
                    Console.WriteLine("Protobuild binary package already present at " + Path.Combine(reference.Folder, platform));
                    return;
                }
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
            GitUtils.MarkIgnored(reference.Folder);

            var package = m_PackageCache.GetBinaryPackage(reference.Uri, reference.GitRef, platform);
            if (package == null)
            {
                this.ResolveLibrarySource(reference, source, forceUpgrade);
                return;
            }

            package.ExtractTo(folder);

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
                catch (ExecEnvironment.SelfInvokeExitException)
                {
                    throw;
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

        private void ResolveTemplateBinary(PackageRef reference, string templateName, string platform, string sourceUri)
        {
            if (reference.Folder != string.Empty)
            {
                throw new InvalidOperationException("Reference folder must be empty for template type.");
            }

            if (Directory.Exists(".staging"))
            {
                PathUtils.AggressiveDirectoryDelete(".staging");
            }

            Directory.CreateDirectory(".staging");

            var package = m_PackageCache.GetBinaryPackage(reference.Uri, reference.GitRef, platform);
            if (package == null)
            {
                this.ResolveTemplateSource(reference, templateName, sourceUri);
                return;
            }

            package.ExtractTo(".staging");

            ApplyProjectTemplateFromStaging(templateName);
        }

        private void ResolveGlobalToolBinary(PackageRef reference, string toolFolder, string platform, bool forceUpgrade)
        {
            if (File.Exists(Path.Combine(toolFolder, ".pkg")))
            {
                if (!forceUpgrade)
                {
                    Console.WriteLine("Protobuild binary package already present at " + toolFolder);
                    return;
                }
            }

            Console.WriteLine("Creating and emptying " + toolFolder);
            this.EmptyReferenceFolder(toolFolder);
            Directory.CreateDirectory(toolFolder);

            Console.WriteLine("Installing " + reference.Uri + " at " + reference.GitRef);
            var package = m_PackageCache.GetBinaryPackage(reference.Uri, reference.GitRef, platform);
            if (package == null)
            {
                Console.WriteLine("The specified global tool package is not available for this platform.");
                return;
            }

            package.ExtractTo(toolFolder);

            var file = File.Create(Path.Combine(toolFolder, ".pkg"));
            file.Close();

            this.m_PackageGlobalTool.ScanPackageForToolsAndInstall(toolFolder);

            Console.WriteLine("Binary resolution complete");
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

            PathUtils.AggressiveDirectoryDelete(".staging");
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

        private void EmptyReferenceFolder(string folder)
        {
            PathUtils.AggressiveDirectoryDelete(folder);
        }
    }
}

