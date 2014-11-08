using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Protobuild
{
    public class ContentProjectGenerator : IContentProjectGenerator
    {
        private readonly ILogger m_Logger;

        public ContentProjectGenerator(ILogger logger)
        {
            this.m_Logger = logger;
        }

        public XmlDocument Generate(string platformName, XmlDocument source, string rootPath)
        {
            var allFiles = new List<KeyValuePair<string, IEnumerable<string>>>();
            string sourceFile = null;
            string sourceFileFolder = null;
            foreach (var element in source
                .DocumentElement
                .ChildNodes
                .OfType<XmlElement>()
                .Where(x => x.Name == "Source"))
            {
                var sourceFolder = element.GetAttribute("Include");
                //Pattern matching to enable platform specific content
                if (sourceFolder.Contains("$(Platform)"))
                {
                    sourceFolder = sourceFolder.Replace("$(Platform)", platformName);
                }
                var matchFiles = element.GetAttribute("Match");
                var originalSourceFolder = sourceFolder;
                if (element.HasAttribute("Primary") && element.GetAttribute("Primary").ToLower() == "true")
                {
                    sourceFileFolder = Path.Combine(rootPath, sourceFolder);
                    sourceFile = Path.Combine(rootPath, sourceFolder, ".source");
                    using (var writer = new StreamWriter(sourceFile))
                    {
                        var dir = new DirectoryInfo(sourceFileFolder);
                        writer.Write(dir.FullName);
                    }
                }
                sourceFolder = Path.Combine(rootPath, sourceFolder);
                var files = this.GetListOfFilesInDirectory(sourceFolder, matchFiles);
                allFiles.Add(
                    new KeyValuePair<string, IEnumerable<string>>(
                        originalSourceFolder,
                        files));
                this.m_Logger.Log(
                    "Scanning: " +
                    originalSourceFolder +
                    " (" + files.Count + " total files)"
                );
            }

            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var projectNode = doc.CreateElement("ContentProject");
            doc.AppendChild(projectNode);
            projectNode.SetAttribute(
                "Name",
                source.DocumentElement.GetAttribute("Name"));

            foreach (var kv in allFiles)
            {
                var originalSourceFolder = kv.Key;
                foreach (var file in kv.Value)
                {
                    var fileNode = doc.CreateElement("Compiled");
                    var fullPathNode = doc.CreateElement("FullPath");
                    var relativePathNode = doc.CreateElement("RelativePath");
                    fullPathNode.AppendChild(doc.CreateTextNode(file));
                    var index = file.Replace("\\", "/")
                        .LastIndexOf(originalSourceFolder.Replace("\\", "/"));
                    var relativePath = "\\" + file
                        .Substring(index + originalSourceFolder.Length)
                        .Replace("/", "\\")
                        .Trim('\\');
                    relativePathNode.AppendChild(doc.CreateTextNode(relativePath));
                    fileNode.AppendChild(fullPathNode);
                    fileNode.AppendChild(relativePathNode);
                    projectNode.AppendChild(fileNode);
                }
            }

            if (sourceFile != null)
            {
                var fileNode = doc.CreateElement("Compiled");
                var fullPathNode = doc.CreateElement("FullPath");
                var relativePathNode = doc.CreateElement("RelativePath");
                fullPathNode.AppendChild(doc.CreateTextNode(sourceFile));
                var index = sourceFile.Replace("\\", "/")
                    .LastIndexOf(sourceFileFolder.Replace("\\", "/"));
                var relativePath = "\\" + sourceFile
                    .Substring(index + sourceFileFolder.Length)
                    .Replace("/", "\\")
                    .Trim('\\');
                relativePathNode.AppendChild(doc.CreateTextNode(relativePath));
                fileNode.AppendChild(fullPathNode);
                fileNode.AppendChild(relativePathNode);
                projectNode.AppendChild(fileNode);
            }

            return doc;
        }

        private List<string> GetListOfFilesInDirectory(string folder, string match)
        {
            var result = new List<string>();
            var directoryInfo = new DirectoryInfo(folder);
            foreach (var directory in directoryInfo.GetDirectories())
            {
                result.AddRange(
                    this.GetListOfFilesInDirectory(directory.FullName, match));
            }
            foreach (var file in directoryInfo.GetFiles(match))
            {
                result.Add(file.FullName);
            }
            return result;
        }
    }
}

