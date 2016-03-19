using System;
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    internal class PlatformResourcesGenerator : IPlatformResourcesGenerator
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;

        public PlatformResourcesGenerator(IHostPlatformDetector hostPlatformDetector)
        {
            _hostPlatformDetector = hostPlatformDetector;
        }

        public void GenerateInfoPListIfNeeded(List<LoadedDefinitionInfo> definitions, DefinitionInfo definition, XmlDocument project, string platform)
        {
            if (platform == "iOS" || platform == "MacOS")
            {
                var type = project.DocumentElement.GetAttribute("Type");
                if (type == "Console" || type == "App")
                {
					var references = project.DocumentElement.SelectNodes("References/*").OfType<XmlElement>();
					foreach (var reference in references)
					{
						var lookup = definitions.FirstOrDefault(x => x.Definition.Name == reference.GetAttribute("Include"));
						if (lookup != null && lookup.Definition.Type == "Include")
						{
							if (project.DocumentElement.SelectSingleNode("Files/*[@Include='Info.plist']") == null)
							{
								return;
							}
						}
					}

                    if (project.DocumentElement.SelectSingleNode("Files/*[@Include='Info.plist']") == null)
                    {
                        // We need to generate an Info.plist file for iOS and Mac; we do this
                        // just with a default Info.plist file which is enough for projects
                        // to compile.
                        var infoPListPath = Path.Combine(definition.AbsolutePath, "Info.plist").Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
                        if (!File.Exists(infoPListPath))
                        {
                            var contents = @"
<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict/>
</plist>
".Trim();
                            using (var writer = new StreamWriter(infoPListPath))
                            {
                                writer.Write(contents);
                            }
                        }

                        var files = project.DocumentElement.SelectSingleNode("Files");
                        var infoPList = project.CreateElement("None");
                        infoPList.SetAttribute("Include", "Info.plist");
                        var platforms = project.CreateElement("Platforms");
                        platforms.InnerText = "iOS,MacOS";
                        infoPList.AppendChild(platforms);
                        files.AppendChild(infoPList);
                    }
                }
            }
            else if (platform == "Android" || platform == "Ouya")
            {
                var type = project.DocumentElement.GetAttribute("Type");
                var files = project.DocumentElement.SelectSingleNode("Files");

                Directory.CreateDirectory(
                    Path.Combine(definition.AbsolutePath, "Resources")
                        .Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar));

                if (type == "Console" || type == "App")
                {
                    Directory.CreateDirectory(
                        Path.Combine(definition.AbsolutePath, "Properties")
                            .Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar));

                    // We need to generate an AndroidManifest.xml file; we do this just with
                    // a default AndroidManifest file which is enough for projects to compile.
                    var manifestPath =
                        Path.Combine(definition.AbsolutePath, "Properties\\AndroidManifest.xml")
                            .Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
                    if (!File.Exists(manifestPath))
                    {
                        var contents = (@"
<?xml version=""1.0"" encoding=""utf-8""?>
<manifest xmlns:android=""http://schemas.android.com/apk/res/android"" package=""" + definition.Name +
                                       @""" android:versionCode=""1"" android:versionName=""1.0"">
    <uses-sdk />
	<application android:label=""" + definition.Name + @"""></application>
</manifest>
").Trim();
                        using (var writer = new StreamWriter(manifestPath))
                        {
                            writer.Write(contents);
                        }
                    }

                    if (files != null)
                    {
                        var manifestNode =
                            project.DocumentElement.SelectSingleNode(
                                "Files/*[@Include='Properties\\AndroidManifest.xml']");
                        if (manifestNode != null && manifestNode.Name != "Content" && manifestNode.ParentNode != null)
                        {
                            manifestNode.ParentNode.RemoveChild(manifestNode);
                            manifestNode = null;
                        }
                        if (manifestNode == null)
                        {
                            var manifest = project.CreateElement("Content");
                            manifest.SetAttribute("Include", "Properties\\AndroidManifest.xml");
                            var platforms = project.CreateElement("Platforms");
                            platforms.InnerText = "Android,Ouya";
                            manifest.AppendChild(platforms);
                            files.AppendChild(manifest);
                        }
                    }
                }

                // We need to generate an empty Resources\Resource.Designer.cs file; we do this just with
                // a default Resource.Designer.cs file which is enough for projects to compile.
                var resourcePath = Path.Combine(definition.AbsolutePath, "Resources\\Resource.Designer.cs").Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
                if (!File.Exists(resourcePath))
                {
                    var contents = string.Empty;
                    using (var writer = new StreamWriter(resourcePath))
                    {
                        writer.Write(contents);
                    }
                }
                    
                if (files != null)
                {
                    var resourceNode =
                        project.DocumentElement.SelectSingleNode(
                            "Files/*[@Include='Resources\\Resource.Designer.cs']");
                    if (resourceNode != null && resourceNode.Name != "Compile" && resourceNode.ParentNode != null)
                    {
                        resourceNode.ParentNode.RemoveChild(resourceNode);
                        resourceNode = null;
                    }
                    if (resourceNode == null)
                    {
                        var resource = project.CreateElement("Compile");
                        resource.SetAttribute("Include", "Resources\\Resource.Designer.cs");
                        var platforms = project.CreateElement("Platforms");
                        platforms.InnerText = "Android,Ouya";
                        resource.AppendChild(platforms);
                        files.AppendChild(resource);
                    }
                }
            }
        }
    }
}

