using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml;

namespace Protobuild.Internal
{
    internal class LocalNuGet3PackageProtocol : IPackageProtocol
    {
        private readonly BinaryPackageResolve _binaryPackageResolve;
        private readonly SourcePackageResolve _sourcePackageResolve;

        public LocalNuGet3PackageProtocol(
            BinaryPackageResolve binaryPackageResolve,
            SourcePackageResolve sourcePackageResolve)
        {
            _binaryPackageResolve = binaryPackageResolve;
            _sourcePackageResolve = sourcePackageResolve;
        }

        public string[] Schemes => new[] { "local-nuget-v3" };

        public IPackageMetadata ResolveSource(string workingDirectory, PackageRequestRef request)
        {
            var path = request.Uri.Substring("local-nuget-v3://".Length);

            string packageName;
            string packageType;
            string sourceCodeUrl;
            string version;
            string binaryFormat;
            string binaryUri;
            string commitHashForSourceResolve;

            // Figure out the package type by looking at the tags inside
            // the package's .nuspec file.
            using (var storer = ZipStorer.Open(path, FileAccess.Read))
            {
                var entries = storer.ReadCentralDir();

                var nuspecEntries = entries.Where(
                    x => x.FilenameInZip.EndsWith(".nuspec") &&
                         !x.FilenameInZip.Contains("/") &&
                         !x.FilenameInZip.Contains("\\")).ToList();

                if (nuspecEntries.Count != 0)
                {
                    using (var stream = new MemoryStream())
                    {
                        storer.ExtractFile(nuspecEntries[0], stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        var document = new XmlDocument();
                        document.Load(stream);

                        var ns = new XmlNamespaceManager(document.NameTable);
                        ns.AddNamespace("x", "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd");

                        packageName = document.SelectSingleNode("//x:id", ns)?.InnerText;
                        version = document.SelectSingleNode("//x:version", ns)?.InnerText;
                        var tags = document.SelectSingleNode("//x:tags", ns)?.InnerText?.Split(new[] { ' ' }) ?? new string[0];

                        packageType = PackageManager.PACKAGE_TYPE_LIBRARY;
                        commitHashForSourceResolve = null;
                        sourceCodeUrl = null;

                        foreach (var tag in tags)
                        {
                            if (!string.IsNullOrWhiteSpace(tag))
                            {
                                if (tag == "type=global-tool")
                                {
                                    packageType = PackageManager.PACKAGE_TYPE_GLOBAL_TOOL;
                                }
                                else if (tag == "type=template")
                                {
                                    packageType = PackageManager.PACKAGE_TYPE_TEMPLATE;
                                }

                                if (tag.StartsWith("commit="))
                                {
                                    commitHashForSourceResolve = tag.Substring("commit=".Length);
                                }

                                if (tag.StartsWith("git="))
                                {
                                    sourceCodeUrl = tag.Substring("git=".Length);
                                }
                            }
                        }

                        binaryUri = path;
                        binaryFormat = PackageManager.ARCHIVE_FORMAT_NUGET_ZIP;
                    }
                }
                else
                {
                    throw new InvalidOperationException("NuGet package is missing nuspec file!");
                }
            }

            return new NuGet3PackageMetadata(
                null,
                packageName,
                packageType,
                sourceCodeUrl,
                request.Platform,
                version,
                binaryFormat,
                binaryUri,
                commitHashForSourceResolve,
                (workingDirectoryAlt, metadata, folder, name, upgrade, source) =>
                {
                    if (source == true)
                    {
                        _sourcePackageResolve.Resolve(workingDirectoryAlt, metadata, folder, name, upgrade);
                    }
                    else
                    {
                        _binaryPackageResolve.Resolve(workingDirectoryAlt, metadata, folder, name, upgrade);
                    }
                });
        }
    }
}
