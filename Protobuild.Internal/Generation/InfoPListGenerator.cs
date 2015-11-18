using System;
using System.Xml;
using System.IO;

namespace Protobuild
{
    public class InfoPListGenerator : IInfoPListGenerator
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;

        public InfoPListGenerator(IHostPlatformDetector hostPlatformDetector)
        {
            _hostPlatformDetector = hostPlatformDetector;
        }

        public void GenerateInfoPListIfNeeded(DefinitionInfo definition, XmlDocument project, string platform)
        {
            if (platform == "iOS" || platform == "MacOS")
            {
                var type = project.DocumentElement.GetAttribute("Type");
                if (type == "Console" || type == "App" || type == "GUI" || type == "GTK")
                {
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
                        files.AppendChild(infoPList);
                    }
                }
            }
        }
    }
}

