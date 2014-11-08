using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Protobuild
{
    public class NuGetRepositoriesConfigGenerator : INuGetRepositoriesConfigGenerator
    {
        public void Generate(
            string solutionPath,
            IEnumerable<string> repositoryPaths)
        {
            FileInfo repositoriesFile = new FileInfo(
                Path.Combine(
                    new FileInfo(solutionPath).Directory.FullName, 
                    "packages", 
                    "repositories.config"));
            Uri repositoriesUri = new Uri(repositoriesFile.FullName);

            // Always refresh this file.
            if (repositoriesFile.Exists)
                repositoriesFile.Delete();
            else if (!repositoriesFile.Directory.Exists)
                repositoriesFile.Directory.Create();

            // Write out the xml to disk.
            using (var writer = new StreamWriter(repositoriesFile.FullName))
            {
                writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                writer.WriteLine("<repositories>");
                foreach (string path in repositoryPaths.OrderBy(p => p))
                {
                    writer.Write("  <repository path=\"");

                    // Write a relational path to the config from the repository.config.
                    Uri current = new Uri(path);
                    writer.Write(
                        Uri.UnescapeDataString(
                            repositoriesUri.MakeRelativeUri(current)
                            .ToString()
                            .Replace('/', Path.DirectorySeparatorChar)));
                    writer.WriteLine("\" />");
                }
                writer.WriteLine("</repositories>");
            }
        }
    }
}

