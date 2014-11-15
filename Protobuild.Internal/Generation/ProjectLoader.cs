using System;
using System.Xml;
using Protobuild.Tasks;
using System.Linq;
using System.Text;

namespace Protobuild
{
    public class ProjectLoader : IProjectLoader
    {
        private readonly IContentProjectGenerator m_ContentProjectGenerator;

        public ProjectLoader(IContentProjectGenerator contentProjectGenerator)
        {
            this.m_ContentProjectGenerator = contentProjectGenerator;
        }

        public XmlDocument Load(string path, string platformName, string rootPath, string modulePath)
        {
            var doc = new XmlDocument();
            doc.Load(path);

            // If this is a ContentProject, we actually need to generate the
            // full project node from the files that are in the Source folder.
            XmlDocument newDoc = null;
            if (doc.DocumentElement.Name == "ContentProject")
                newDoc = this.m_ContentProjectGenerator.Generate(platformName, doc, modulePath);
            else
                newDoc = doc;
            if (rootPath != null && modulePath != null)
            {
                var additionalPath = modulePath.Substring(rootPath.Length).Replace('/', '\\');
                if (newDoc.DocumentElement != null &&
                    newDoc.DocumentElement.Attributes["Path"] != null &&
                    additionalPath != null)
                {
                    newDoc.DocumentElement.Attributes["Path"].Value =
                        (additionalPath.Trim('\\') + '\\' +
                            newDoc.DocumentElement.Attributes["Path"].Value).Replace('/', '\\').Trim('\\');
                }
                if (newDoc.DocumentElement.Name == "ExternalProject")
                {
                    // Need to find all descendant Binary and Project tags
                    // and update their paths as well.
                    var xDoc = newDoc.ToXDocument();
                    var projectsToUpdate = xDoc.Descendants().Where(x => x.Name == "Project");
                    var binariesToUpdate = xDoc.Descendants().Where(x => x.Name == "Binary");
                    var nativeBinariesToUpdate = xDoc.Descendants().Where(x => x.Name == "NativeBinary");
                    foreach (var pathToUpdate in projectsToUpdate.Concat(binariesToUpdate).Concat(nativeBinariesToUpdate)
                        .Where(x => x.Attribute("Path") != null))
                    {
                        pathToUpdate.Attribute("Path").Value =
                            (additionalPath.Trim('\\') + '\\' +
                                pathToUpdate.Attribute("Path").Value).Replace('/', '\\').Trim('\\');
                    }
                    newDoc = xDoc.ToXmlDocument();
                }
            }

            // If the Guid property doesn't exist, we do one of two things:
            //  * Check for the existance of a Guid under the ProjectGuids tag
            //  * Autogenerate a Guid for the project
            if (doc.DocumentElement.Attributes["Guid"] == null)
            {
                var autogenerate = true;
                var projectGuids = doc.DocumentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name == "ProjectGuids");
                if (projectGuids != null)
                {
                    var platform = projectGuids.ChildNodes.OfType<XmlElement>().FirstOrDefault(x =>
                        x.Name == "Platform" && x.HasAttribute("Name") && x.GetAttribute("Name") == platformName);
                    if (platform != null)
                    {
                        autogenerate = false;
                        doc.DocumentElement.SetAttribute("Guid", platform.InnerText.Trim().ToUpper());
                    }
                }

                if (autogenerate)
                {
                    var name = doc.DocumentElement.GetAttribute("Name") + "." + platformName;
                    var guidBytes = new byte[16];
                    for (var i = 0; i < guidBytes.Length; i++)
                        guidBytes[i] = (byte)0;
                    var nameBytes = Encoding.ASCII.GetBytes(name);
                    unchecked
                    {
                        for (var i = 0; i < nameBytes.Length; i++)
                            guidBytes[i % 16] += nameBytes[i];
                        for (var i = nameBytes.Length; i < 16; i++)
                            guidBytes[i] += nameBytes[i % nameBytes.Length];
                    }
                    var guid = new Guid(guidBytes);
                    doc.DocumentElement.SetAttribute("Guid", guid.ToString().ToUpper());
                }
            }

            return newDoc;
        }
    }
}

