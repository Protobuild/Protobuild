using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protobuild
{
    internal class PackPackageCommand : ICommand
    {
        private readonly IAutomaticModulePackager m_AutomaticProjectPackager;

        private readonly IFileFilterParser m_FileFilterParser;

        private readonly IHostPlatformDetector m_HostPlatformDetector;

        private readonly IDeduplicator m_Deduplicator;

        private readonly IPackageCreator _packageCreator;

        private readonly IGetRecursiveUtilitiesInPath _getRecursiveUtilitiesInPath;

        private readonly IFeatureManager _featureManager;

        public PackPackageCommand(
            IAutomaticModulePackager automaticProjectPackager,
            IFileFilterParser fileFilterParser,
            IHostPlatformDetector hostPlatformDetector,
            IDeduplicator deduplicator,
            IPackageCreator packageCreator,
            IGetRecursiveUtilitiesInPath getRecursiveUtilitiesInPath,
            IFeatureManager featureManager)
        {
            this.m_AutomaticProjectPackager = automaticProjectPackager;
            this.m_HostPlatformDetector = hostPlatformDetector;
            this.m_FileFilterParser = fileFilterParser;
            this.m_Deduplicator = deduplicator;
            _packageCreator = packageCreator;
            _getRecursiveUtilitiesInPath = getRecursiveUtilitiesInPath;
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length < 3 || args[0] == null || args[1] == null || args[2] == null)
            {
                throw new InvalidOperationException("You must provide the module folder, destination file and platform to -pack.");
            }

            pendingExecution.PackageSourceFolder = new DirectoryInfo(args[0]).FullName;
            pendingExecution.PackageDestinationFile = new FileInfo(args[1]).FullName;
            pendingExecution.PackagePlatform = args.Length >= 3 ? args[2] : this.m_HostPlatformDetector.DetectPlatform();
            pendingExecution.PackageFilterFile = args.Length >= 4 ? args[3] : null;
        }

        public int Execute(Execution execution)
        {
            if (!Directory.Exists(execution.PackageSourceFolder))
            {
                throw new InvalidOperationException("The source folder " + execution.PackageSourceFolder + " does not exist.");
            }

            if (execution.PackagePlatform == "Template")
            {
                return ExecuteForTemplate(execution);
            }

            var allowAutopackage = true;
            var moduleExpectedPath = Path.Combine(execution.PackageSourceFolder, "Build", "Module.xml");
            ModuleInfo rootModule = null;
            if (!File.Exists(moduleExpectedPath))
            {
                if (execution.PackageFilterFile == null)
                {
                    Console.WriteLine(
                        "There is no module in the path '" + execution.PackageSourceFolder + "' (expected to " +
                        "find a Build\\Module.xml file within that directory).");
                    return 1;
                }
                else
                {
                    // We allow this mode if the user has provided a filter file and are constructing
                    // the package manually.
                    allowAutopackage = false;
                }
            }
            else
            {
                rootModule = ModuleInfo.Load(moduleExpectedPath);
            }

            var temporaryFiles = new List<string>();
            try
            {
                var customDirectives = new Dictionary<string, Action<FileFilter>>()
                {
                    {
                        "autopackage",
                        f =>
                        {
                            if (allowAutopackage && rootModule != null)
                            {
                                this.m_AutomaticProjectPackager.Autopackage(
                                    f,
                                    execution,
                                    rootModule,
                                    execution.PackageSourceFolder,
                                    execution.PackagePlatform,
                                    temporaryFiles);
                            }
                            else
                            {
                                Console.WriteLine(
                                    "WARNING: There is no module in the path '" + execution.PackageSourceFolder +
                                    "' (expected to " +
                                    "find a Build\\Module.xml file within that directory).  Ignoring the 'autopackage' directive.");
                            }
                        }
                    }
                };

                Console.WriteLine("Starting package creation for " + execution.PackagePlatform);

                var filter =
                    new FileFilter(_getRecursiveUtilitiesInPath.GetRecursiveFilesInPath(execution.PackageSourceFolder));
                if (execution.PackageFilterFile != null)
                {
                    using (var reader = new StreamReader(execution.PackageFilterFile))
                    {
                        var contents = reader.ReadToEnd();
                        contents = contents.Replace("%PLATFORM%", execution.PackagePlatform);

                        using (var inputStream = new MemoryStream(Encoding.ASCII.GetBytes(contents)))
                        {
                            this.m_FileFilterParser.ParseAndApply(filter, inputStream, customDirectives);
                        }
                    }
                }
                else
                {
                    customDirectives["autopackage"](filter);
                }

                if (File.Exists(execution.PackageDestinationFile))
                {
                    Console.WriteLine("The destination file " + execution.PackageDestinationFile +
                                      " already exists; it will be overwritten.");
                    File.Delete(execution.PackageDestinationFile);
                }

                filter.ImplyDirectories();

                var filterDictionary = filter.ToDictionary(k => k.Key, v => v.Value);

                if (!filterDictionary.ContainsValue("Build/"))
                {
                    Console.WriteLine("ERROR: The Build directory does not exist in the source folder.");
                    if (execution.PackageFilterFile != null)
                    {
                        this.PrintFilterMappings(filterDictionary);
                    }
                    return 1;
                }

                if (!filterDictionary.ContainsValue("Build/Projects/"))
                {
                    Console.WriteLine("ERROR: The Build\\Projects directory does not exist in the source folder.");
                    if (execution.PackageFilterFile != null)
                    {
                        this.PrintFilterMappings(filterDictionary);
                    }
                    return 1;
                }

                if (!filterDictionary.ContainsValue("Build/Module.xml"))
                {
                    Console.WriteLine("ERROR: The Build\\Module.xml file does not exist in the source folder.");
                    if (execution.PackageFilterFile != null)
                    {
                        this.PrintFilterMappings(filterDictionary);
                    }
                    return 1;
                }

                if (filterDictionary.ContainsValue("Protobuild.exe"))
                {
                    Console.WriteLine("ERROR: The Protobuild.exe file should not be included in the package file.");
                    if (execution.PackageFilterFile != null)
                    {
                        this.PrintFilterMappings(filterDictionary);
                    }
                    return 1;
                }

                using (
                    var target = new FileStream(execution.PackageDestinationFile, FileMode.CreateNew, FileAccess.Write,
                        FileShare.None))
                {
                    _packageCreator.Create(
                        target,
                        filter,
                        execution.PackageSourceFolder,
                        execution.PackageFormat);
                }

                Console.WriteLine("\rPackage written to " + execution.PackageDestinationFile + " successfully.");
                return 0;
            }
            finally
            {
                foreach (var file in temporaryFiles)
                {
                    File.Delete(file);
                }
            }
        }

        public int ExecuteForTemplate(Execution execution)
        {
            if (!Directory.Exists(execution.PackageSourceFolder))
            {
                throw new InvalidOperationException("The source folder " + execution.PackageSourceFolder + " does not exist.");
            }

            Console.WriteLine("Starting package creation for " + execution.PackagePlatform);

            var filter = new FileFilter(_getRecursiveUtilitiesInPath.GetRecursiveFilesInPath(execution.PackageSourceFolder));
            if (execution.PackageFilterFile != null)
            {
                using (var reader = new StreamReader(execution.PackageFilterFile))
                {
                    var contents = reader.ReadToEnd();
                    contents = contents.Replace("%PLATFORM%", execution.PackagePlatform);

                    using (var inputStream = new MemoryStream(Encoding.ASCII.GetBytes(contents)))
                    {
                        this.m_FileFilterParser.ParseAndApply(filter, inputStream, new Dictionary<string, Action<FileFilter>>());
                    }
                }
            }
            else
            {
                filter.ApplyInclude("^.*$");
                filter.ApplyExclude("^\\.git/.*$");
                filter.ApplyExclude("^\\.hg/.*$");
                filter.ApplyExclude("^\\.svn/.*$");
            }

            if (File.Exists(execution.PackageDestinationFile))
            {
                Console.WriteLine("The destination file " + execution.PackageDestinationFile + " already exists; it will be overwritten.");
                File.Delete(execution.PackageDestinationFile);
            }

            filter.ImplyDirectories();

            var filterDictionary = filter.ToDictionary(k => k.Key, v => v.Value);

            if (!filterDictionary.ContainsValue("Build/"))
            {
                Console.WriteLine("ERROR: The Build directory does not exist in the source folder.");
                if (execution.PackageFilterFile != null)
                {
                    this.PrintFilterMappings(filterDictionary);
                }
                return 1;
            }

            if (!filterDictionary.ContainsValue("Build/Projects/"))
            {
                Console.WriteLine("ERROR: The Build\\Projects directory does not exist in the source folder.");
                if (execution.PackageFilterFile != null)
                {
                    this.PrintFilterMappings(filterDictionary);
                }
                return 1;
            }

            if (!filterDictionary.ContainsValue("Build/Module.xml"))
            {
                Console.WriteLine("ERROR: The Build\\Module.xml file does not exist in the source folder.");
                if (execution.PackageFilterFile != null)
                {
                    this.PrintFilterMappings(filterDictionary);
                }
                return 1;
            }

            if (filterDictionary.ContainsValue("Protobuild.exe"))
            {
                Console.WriteLine("ERROR: The Protobuild.exe file should not be included in the package file.");
                if (execution.PackageFilterFile != null)
                {
                    this.PrintFilterMappings(filterDictionary);
                }
                return 1;
            }

            using (var target = new FileStream(execution.PackageDestinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                _packageCreator.Create(
                    target,
                    filter,
                    execution.PackageSourceFolder,
                    execution.PackageFormat);
            }

            Console.WriteLine("\rPackage written to " + execution.PackageDestinationFile + " successfully.");
            return 0;
        }

        public string GetDescription()
        {
            return @"
Compresses the specified module into a Protobuild package, assuming
the module has been generated and built as the specified platform.
Change the archive format with the -format option.  -enable and
-disable can be used to indicate what services were explicitly enabled
or disabled when the code was built.  If a filter file is not specified,
automatically packages the module according to the default settings.
If a filter file is specified, performs the steps in the filter file instead.
";
        }

        public int GetArgCount()
        {
            return 4;
        }

        public string[] GetArgNames()
        {
            return new[] { "module_path", "package_file", "platform", "filter?" };
        }

        public bool IsInternal()
        {
            return false;
        }

        public bool IsRecognised()
        {
            return _featureManager.IsFeatureEnabled(Feature.PackageManagement);
        }

        public bool IsIgnored()
        {
            return false;
        }

        private void PrintFilterMappings(Dictionary<string, string> mappings)
        {
            Console.WriteLine("The filter mappings resulted in: ");
            foreach (var kv in mappings)
            {
                Console.WriteLine("  " + kv.Key + " -> " + kv.Value);
            }
        }
    }
}

