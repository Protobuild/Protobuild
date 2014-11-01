using System;
using System.IO;
using System.IO.Compression;

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
        }

        public int Execute(Execution execution)
        {
            if (!Directory.Exists(execution.PackageSourceFolder))
            {
                throw new InvalidOperationException("The source folder " + execution.PackageSourceFolder + " does not exist.");
            }

            if (!Directory.Exists(Path.Combine(execution.PackageSourceFolder, "Build")))
            {
                Console.WriteLine("ERROR: The Build directory does not exist in the source folder.");
                return 1;
            }

            if (!Directory.Exists(Path.Combine(execution.PackageSourceFolder, "Build", "Projects")))
            {
                Console.WriteLine("ERROR: The Build\\Projects directory does not exist in the source folder.");
                return 1;
            }

            if (!File.Exists(Path.Combine(execution.PackageSourceFolder, "Build", "Module.xml")))
            {
                Console.WriteLine("ERROR: The Build\\Module.xml file does not exist in the source folder.");
                return 1;
            }

            if (File.Exists(Path.Combine(execution.PackageSourceFolder, "Protobuild.exe")))
            {
                Console.WriteLine("ERROR: The Protobuild.exe file should not be included in the package file.");
                return 1;
            }

            if (File.Exists(execution.PackageDestinationFile))
            {
                Console.WriteLine("The destination file " + execution.PackageDestinationFile + " already exists; it will be overwritten.");
                File.Delete(execution.PackageDestinationFile);
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
                            var writer = new tar_cs.TarWriter(archive);
                            this.PackDirectoryToArchive(execution.PackageSourceFolder, writer);
                            break;
                        }
                }

                archive.Seek(0, SeekOrigin.Begin);

                switch (execution.PackageFormat)
                {
                    case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                        {
                            using (var compress = new GZipStream(target, CompressionMode.Compress))
                            {
                                archive.CopyTo(compress);
                            }

                            break;
                        }
                    case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                    default:
                        {
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
Change the archive format with the -format option.
";
        }

        public int GetArgCount()
        {
            return 2;
        }

        public string[] GetArgNames()
        {
            return new[] { "folder", "package_file" };
        }

        private void PackDirectoryToArchive(string packageSourceFolder, tar_cs.TarWriter writer, string relative = null)
        {
            var dirInfo = new DirectoryInfo(packageSourceFolder);

            foreach (var dir in dirInfo.GetDirectories())
            {
                var inPackageName = relative == null ? dir.Name : Path.Combine(relative, dir.Name);
                writer.WriteDirectoryEntry(inPackageName);
                this.PackDirectoryToArchive(dir.FullName, writer, inPackageName);
            }

            foreach (var file in dirInfo.GetFiles())
            {
                var inPackageName = relative == null ? file.Name : Path.Combine(relative, file.Name);
                using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    writer.Write(stream, stream.Length, inPackageName);
                }
            }
        }
    }
}

