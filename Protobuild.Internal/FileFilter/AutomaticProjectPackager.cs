using System;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Xml;
using System.IO;
using System.Linq;

namespace Protobuild
{
    public class AutomaticProjectPackager : IAutomaticProjectPackager
    {
        public AutomaticProjectPackager()
        {
        }

        public void AutoProject(FileFilter fileFilter, ModuleInfo rootModule, string platform)
        {
            // The automatic project packager takes the following steps:
            //  1. Load all of the project definitions recursively.
            //  2. Locate all their target C# project files.
            //  3. Read the OutputPath property from those project files (we
            //     do this in case the user is using custom XSLT which outputs
            //     non-standard paths, and also future-proofs this code against
            //     changes to the official XSLT).
            //  4. Determine the assembly name and resulting files that should
            //     already be in the output directory (because the project has
            //     been built).
            //  5. Mark any <name>.dll, <name>.dll.mdb, <name>.pdb, <name>.xml,
            //     <name>.dll.config files for inclusion.
            //  6. Next we scan for any files in the C# project file that are
            //     marked as copy-on-build or are of the Content tag type, and
            //     mark those for inclusion.
            //  7. Rewrite the file paths into directories suitable for the
            //     package.
            //  8. Generate ExternalProjects for each definition (including recursive
            //     ones).

            foreach (var definition in rootModule.GetDefinitionsRecursively(platform))
            {
                if (definition.Type == "External" || definition.Type == "Content")
                {
                    continue;
                }

                switch (definition.Type)
                {
                    case "External":
                        this.AutomaticallyPackageExternalProject(fileFilter, platform, definition);
                        break;
                    case "Content":
                        break;
                    default:
                        this.AutomaticallyPackageNormalProject(fileFilter, platform, definition);
                        break;
                }
            }

            // If there is no Module.xml in the source mappings already, then copy the current module.
            var filterDictionary = fileFilter.ToDictionary(k => k.Key, v => v.Value);
            if (!filterDictionary.ContainsValue("Build/Module.xml"))
            {
                fileFilter.AddManualMapping(Path.Combine(rootModule.Path, "Build", "Module.xml"), "Build/Module.xml");
            }
        }

        private void AutomaticallyPackageExternalProject(FileFilter fileFilter, string platform, DefinitionInfo definition)
        {

        }

        private void AutomaticallyPackageNormalProject(FileFilter fileFilter, string platform, DefinitionInfo definition)
        {
            var document = XDocument.Load(definition.DefinitionPath);
            var platformSpecificOutputFolderElement = document.XPathSelectElement("/Project/Properties/PlatformSpecificOutputFolder");
            var projectSpecificOutputFolderElement = document.XPathSelectElement("/Project/Properties/ProjectSpecificOutputFolder");
            var assemblyNameForPlatformElement = document.XPathSelectElement("/Project/Properties/AssemblyName/Platform[@Name=\"" + platform + "\"]");
            var assemblyNameGlobalElement = document.XPathSelectElement("/Project/Properties/Property[@Name=\"AssemblyName\"]");
            var platformSpecificOutputFolder = true;
            var projectSpecificOutputFolder = false;
            if (platformSpecificOutputFolderElement != null)
            {
                platformSpecificOutputFolder = platformSpecificOutputFolderElement.Value.ToLowerInvariant() != "false";
            }
            if (projectSpecificOutputFolderElement != null)
            {
                projectSpecificOutputFolder = projectSpecificOutputFolderElement.Value.ToLowerInvariant() == "true";
            }
            string assemblyName = null;
            if (assemblyNameForPlatformElement != null)
            {
                assemblyName = assemblyNameForPlatformElement.Value;
            }
            else
                if (assemblyNameGlobalElement != null)
                {
                    assemblyName = assemblyNameGlobalElement.Value;
                }
                else
                {
                    assemblyName = definition.Name;
                }
            var assemblyFilesToCopy = new[] {
                assemblyName + ".exe",
                assemblyName + ".dll",
                assemblyName + ".dll.config",
                assemblyName + ".dll.mdb",
                assemblyName + ".pdb",
                assemblyName + ".xml",
            };
            var outputMode = OutputPathMode.BinConfiguration;
            if (projectSpecificOutputFolder)
            {
                outputMode = OutputPathMode.BinProjectPlatformArchConfiguration;
            }
            if (platformSpecificOutputFolder)
            {
                outputMode = OutputPathMode.BinPlatformArchConfiguration;
            }
            var externalProjectDocument = new XmlDocument();
            externalProjectDocument.AppendChild(externalProjectDocument.CreateXmlDeclaration("1.0", "UTF-8", null));
            var externalProject = externalProjectDocument.CreateElement("ExternalProject");
            externalProjectDocument.AppendChild(externalProject);
            externalProject.SetAttribute("Name", definition.Name);
            switch (outputMode)
            {
                case OutputPathMode.BinConfiguration:
                {
                    // In this configuration, we only ship the binaries for
                    // the default architecture (because that's all we know
                    // about).  We also have to assume the binary folder
                    // contains binaries for the desired platform.
                    var pathPrefix = definition.Path.Replace(".", "\\.") + "/bin/([^/]+)/";
                    foreach (var assemblyFile in assemblyFilesToCopy)
                    {
                        var includeMatch = fileFilter.ApplyInclude("^" + pathPrefix + assemblyFile.Replace(".", "\\.") + "$");
                        var rewriteMatch = fileFilter.ApplyRewrite("^" + pathPrefix + assemblyFile.Replace(".", "\\.") + "$", definition.Name + "/AnyCPU/" + assemblyFile);
                        if (includeMatch && rewriteMatch)
                        {
                            if (assemblyFile.EndsWith(".dll"))
                            {
                                var binaryEntry = externalProjectDocument.CreateElement("Binary");
                                binaryEntry.SetAttribute("Name", assemblyFile.Substring(0, assemblyFile.Length - 4));
                                binaryEntry.SetAttribute("Path", definition.Name + "\\AnyCPU\\" + assemblyFile);
                                externalProject.AppendChild(binaryEntry);
                            }
                            else if (assemblyFile.EndsWith(".dll.config"))
                            {
                                var configEntry = externalProjectDocument.CreateElement("NativeBinary");
                                configEntry.SetAttribute("Path", definition.Name + "\\AnyCPU\\" + assemblyFile);
                                externalProject.AppendChild(configEntry);
                            }
                        }
                        else if (includeMatch || rewriteMatch)
                        {
                            throw new InvalidOperationException("Automatic filter; only one rule matched.");
                        }
                    }
                    break;
                }
                case OutputPathMode.BinPlatformArchConfiguration:
                {
                    // In this configuration, we ship binaries for all
                    // .NET architectures, but only for the current
                    // platform.
                    var pathPrefix = definition.Path.Replace(".", "\\.") + "/bin/" + platform + "/([^/]+)/([^/]+)/";
                    foreach (var assemblyFile in assemblyFilesToCopy)
                    {
                        var includeMatch = fileFilter.ApplyInclude("^" + pathPrefix + assemblyFile.Replace(".", "\\.") + "$");
                        var rewriteMatch = fileFilter.ApplyRewrite("^" + pathPrefix + assemblyFile.Replace(".", "\\.") + "$", definition.Name + "/$1/" + assemblyFile);
                        if (includeMatch && rewriteMatch)
                        {
                            if (assemblyFile.EndsWith(".dll"))
                            {
                                var binaryEntry = externalProjectDocument.CreateElement("Binary");
                                binaryEntry.SetAttribute("Name", assemblyFile.Substring(0, assemblyFile.Length - 4));
                                binaryEntry.SetAttribute("Path", definition.Name + "\\$(Configuration)\\" + assemblyFile);
                                externalProject.AppendChild(binaryEntry);
                            }
                            else if (assemblyFile.EndsWith(".dll.config"))
                            {
                                var configEntry = externalProjectDocument.CreateElement("NativeBinary");
                                configEntry.SetAttribute("Path", definition.Name + "\\$(Configuration)\\" + assemblyFile);
                                externalProject.AppendChild(configEntry);
                            }
                        }
                        else if (includeMatch || rewriteMatch)
                        {
                            throw new InvalidOperationException("Automatic filter; only one rule matched.");
                        }
                    }
                    break;
                }
                case OutputPathMode.BinProjectPlatformArchConfiguration:
                {
                    throw new NotSupportedException();
                    break;
                }
            }
            // Write out the external project to a temporary file and include it.
            var name = Path.GetRandomFileName() + "_" + definition.Name + ".xml";
            var temp = Path.Combine(Path.GetTempPath(), name);
            using (var writer = XmlWriter.Create(temp, new XmlWriterSettings { Indent = true, IndentChars = "  " }))
            {
                externalProjectDocument.WriteTo(writer);
            }
            fileFilter.AddManualMapping(temp, "Build/Projects/" + definition.Name + ".definition");
        }

        private enum OutputPathMode
        {
            BinConfiguration,
            BinPlatformArchConfiguration,
            BinProjectPlatformArchConfiguration,
        }
    }
}

