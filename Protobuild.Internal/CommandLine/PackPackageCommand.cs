using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    public class PackPackageCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length < 2 || args[0] == null || args[1] == null)
            {
                throw new InvalidOperationException("You must provide the source folder and destination file to -pack.");
            }

            pendingExecution.PackageSourceFolder = new DirectoryInfo(args[0]).FullName;
            pendingExecution.PackageDestinationFile = new FileInfo(args[1]).FullName;
            pendingExecution.PackageFilterFile = args.Length >= 3 ? args[2] : null;
        }

        public int Execute(Execution execution)
        {
            if (!Directory.Exists(execution.PackageSourceFolder))
            {
                throw new InvalidOperationException("The source folder " + execution.PackageSourceFolder + " does not exist.");
            }

            FileFilter filter;
            if (execution.PackageFilterFile != null)
            {
                filter = FileFilterParser.Parse(execution.PackageFilterFile, GetRecursiveFilesInPath(execution.PackageSourceFolder));
            }
            else
            {
                filter = new FileFilter(GetRecursiveFilesInPath(execution.PackageSourceFolder));
                filter.ApplyInclude(".*");
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
                var archive = new MemoryStream();

                switch (execution.PackageFormat)
                {
                    case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                    case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    default:
                        {
                            using (var writer = new tar_cs.TarWriter(archive))
                            {
                                // Note that the TAR entries must be in order, with the directories
                                // before files that are within them.  Ordering alphabetically forces
                                // directories to always appear before the files that are in them.
                                foreach (var kv in filter.OrderBy(kv => kv.Value))
                                {
                                    if (kv.Value.EndsWith("/"))
                                    {
                                        // Directory
                                        writer.WriteDirectoryEntry(kv.Value.TrimEnd('/'));
                                    }
                                    else
                                    {
                                        // File
                                        var realFile = Path.Combine(execution.PackageSourceFolder, kv.Key);
                                        using (var stream = new FileStream(realFile, FileMode.Open, FileAccess.Read, FileShare.None))
                                        {
                                            writer.Write(stream, stream.Length, kv.Value);
                                        }
                                    }
                                }
                            }

                            break;
                        }
                }

                archive.Seek(0, SeekOrigin.Begin);

                switch (execution.PackageFormat)
                {
                    case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                        {
                            Console.WriteLine("Writing package in tar/gzip format...");

                            using (var compress = new GZipStream(target, CompressionMode.Compress))
                            {
                                archive.CopyTo(compress);
                            }

                            break;
                        }
                    case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    default:
                        {
                            Console.WriteLine("Writing package in tar/lzma format...");
                            LZMA.LzmaHelper.Compress(archive, target);
                            break;
                        }
                }
            }

            Console.WriteLine("Package written to " + execution.PackageDestinationFile + " successfully.");
            return 0;
        }

        public string GetDescription()
        {
            return @"
Compresses the specified folder into a Protobuild package.  Validates
that the folder contains the correct project structure before packing.
Change the archive format with the -format option.  If a filter file
is not specified, includes all files within the folder.
";
        }

        public int GetArgCount()
        {
            return 3;
        }

        public string[] GetArgNames()
        {
            return new[] { "folder", "package_file", "filter" };
        }

        private static IEnumerable<string> GetRecursiveFilesInPath(string path)
        {
            var current = new DirectoryInfo(path);

            foreach (var di in current.GetDirectories())
            {
                foreach (string s in GetRecursiveFilesInPath(path + "/" + di.Name))
                {
                    yield return (di.Name + "/" + s).Trim('/');
                }
            }

            foreach (var fi in current.GetFiles())
            {
                yield return fi.Name;
            }
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

