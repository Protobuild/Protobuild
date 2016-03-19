using System;
using System.Xml;
using Protobuild.Tasks;
using System.Linq;
using System.Text;

namespace Protobuild
{
    internal class ProjectLoader : IProjectLoader
    {
        private readonly IContentProjectGenerator m_ContentProjectGenerator;

        public ProjectLoader(IContentProjectGenerator contentProjectGenerator)
        {
            this.m_ContentProjectGenerator = contentProjectGenerator;
        }

        public LoadedDefinitionInfo Load(string targetPlatform, ModuleInfo module, DefinitionInfo definition)
        {
            var doc = new XmlDocument();
            doc.Load(definition.DefinitionPath);

            // If this is a ContentProject, we actually need to generate the
            // full project node from the files that are in the Source folder.
            XmlDocument newDoc = null;
            if (doc.DocumentElement.Name == "ContentProject")
                newDoc = this.m_ContentProjectGenerator.Generate(targetPlatform, doc, definition.ModulePath);
            else
                newDoc = doc;
            if (module.Path != null && definition.ModulePath != null)
            {
                var additionalPath = PathUtils.GetRelativePath(
                    module.Path.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar,
                    definition.ModulePath.TrimEnd(System.IO.Path.DirectorySeparatorChar) + System.IO.Path.DirectorySeparatorChar);

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
                    var toolsToUpdate = xDoc.Descendants().Where(x => x.Name == "Tool");
                    foreach (var pathToUpdate in projectsToUpdate.Concat(binariesToUpdate).Concat(nativeBinariesToUpdate).Concat(toolsToUpdate)
                        .Where(x => x.Attribute("Path") != null))
                    {
                        pathToUpdate.Attribute("Path").Value =
                            (additionalPath.Trim('\\') + '\\' +
                                pathToUpdate.Attribute("Path").Value).Replace('/', '\\').Trim('\\');
                    }
                    newDoc = xDoc.ToXmlDocument();
                }
            }

            // If the ProjectGuids element doesn't exist, create it.
            var projectGuids = doc.DocumentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name == "ProjectGuids");
            if (projectGuids == null)
            {
                projectGuids = doc.CreateElement("ProjectGuids");
                doc.DocumentElement.AppendChild(projectGuids);
            }

            // For all the supported platforms of this project, or all the
            // supported platforms of the module, generate GUIDs for any platform
            // that doesn't already exist.
            var platforms = doc.DocumentElement.GetAttribute("Platforms");
            if (string.IsNullOrWhiteSpace(platforms))
            {
                platforms = module.SupportedPlatforms;
            }
            if (string.IsNullOrWhiteSpace(platforms))
            {
                platforms = ModuleInfo.GetSupportedPlatformsDefault();
            }
			var platformsList = platforms.Split(',').ToList();
			if (!platformsList.Contains(targetPlatform))
			{
				platformsList.Add(targetPlatform);
			}
			foreach (var platform in platformsList)
            {
                var existing = projectGuids.ChildNodes.OfType<XmlElement>().FirstOrDefault(x =>
                    x.Name == "Platform" && x.HasAttribute("Name") && x.GetAttribute("Name") == platform);
                if (existing == null)
                {
                    var platformElement = doc.CreateElement("Platform");
                    platformElement.SetAttribute("Name", platform);

                    var name = doc.DocumentElement.GetAttribute("Name") + "." + platform;
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
                    platformElement.InnerText = guid.ToString().ToUpper();
                    projectGuids.AppendChild(platformElement);
                }
            }

            // If the Language property doesn't exist, set it to the default of "C#"
            if (doc.DocumentElement.Attributes["Language"] == null)
            {
                doc.DocumentElement.SetAttribute("Language", "C#");
            }

            // Normalize the project type for backwards compatibility.
            var projectType = doc.DocumentElement.Attributes["Type"];
            if (projectType == null)
            {
                doc.DocumentElement.SetAttribute("Type", "Library");
            }
            else
            {
                switch (projectType.Value)
                {
                    case "Library":
                    case "App":
                    case "Console":
                    case "Website":
                        break;
                    case "GUI":
                    case "XNA":
                    case "GTK":
                        doc.DocumentElement.SetAttribute("Type", "App");
                        break;
                }
            }

            return new LoadedDefinitionInfo
            {
                Definition = definition,
                Project = newDoc,
            };
        }
    }
}

