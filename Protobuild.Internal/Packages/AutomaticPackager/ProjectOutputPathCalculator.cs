using System;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Protobuild
{
    internal class ProjectOutputPathCalculator : IProjectOutputPathCalculator
    {
        public string GetProjectOutputPathPrefix(string platform, DefinitionInfo definition, XDocument document, bool asRegex)
        {
            var outputMode = this.GetProjectOutputMode(document);

            // Copy the assembly itself out to the package.
            switch (outputMode)
            {
            case OutputPathMode.BinConfiguration:
                {
                    // In this configuration, we only ship the binaries for
                    // the default architecture (because that's all we know
                    // about).  We also have to assume the binary folder
                    // contains binaries for the desired platform.
                    if (asRegex)
                    {
                        return definition.RelativePath.Replace('\\', '/').Replace(".", "\\.") + "/bin/([^/]+)/";
                    }
                    else
                    {
                        return definition.RelativePath.Replace('\\', '/') + "/bin/";
                    }
                }
            case OutputPathMode.BinPlatformArchConfiguration:
                {
                    // In this configuration, we ship binaries for AnyCPU, iPhoneSimulator or all .NET architectures
                    // depending on whether or not the platform produces multiple architectures.  On Mono,
                    // we can't use $(Platform) within a reference's path, so we have to keep this path static
                    // for Mono platforms.
                    string pathArchMatch;
                    switch (platform.ToLowerInvariant())
                    {
                    case "ios":
                        {
                            pathArchMatch = "iPhoneSimulator";
                            break;
                        }
                    case "windowsphone":
                        {
                            pathArchMatch = "([^/]+)";
                            break;
                        }
                    default:
                        {
                            pathArchMatch = "AnyCPU";
                            break;
                        }
                    }

                    if (asRegex)
                    {
                        return definition.RelativePath.Replace('\\', '/').Replace(".", "\\.") + "/bin/" + platform + "/" + pathArchMatch + "/([^/]+)/";
                    }
                    else
                    {
                        return definition.RelativePath.Replace('\\', '/') + "/bin/" + platform + "/" + pathArchMatch + "/";
                    }
                }
            case OutputPathMode.BinProjectPlatformArchConfiguration:
                {
                    throw new NotSupportedException();
                }
            }

            throw new NotSupportedException();
        }

        public string GetProjectAssemblyName(string platform, DefinitionInfo definition, XDocument document)
        {
            var assemblyNameForPlatformElement = document.XPathSelectElement("/Project/Properties/AssemblyName/Platform[@Name=\"" + platform + "\"]");
            var assemblyNameGlobalElement = document.XPathSelectElement("/Project/Properties/Property[@Name=\"AssemblyName\"]");

            string assemblyName = null;
            if (assemblyNameForPlatformElement != null)
            {
                assemblyName = assemblyNameForPlatformElement.Value;
            }
            else if (assemblyNameGlobalElement != null)
            {
                assemblyName = assemblyNameGlobalElement.Attribute(XName.Get("Value")).Value;
            }
            else
            {
                assemblyName = definition.Name;
            }

            return assemblyName;
        }

        public OutputPathMode GetProjectOutputMode(XDocument document)
        {
            var platformSpecificOutputFolderElement = document.XPathSelectElement("/Project/Properties/PlatformSpecificOutputFolder");
            var projectSpecificOutputFolderElement = document.XPathSelectElement("/Project/Properties/ProjectSpecificOutputFolder");
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

            var outputMode = OutputPathMode.BinConfiguration;
            if (projectSpecificOutputFolder)
            {
                outputMode = OutputPathMode.BinProjectPlatformArchConfiguration;
            }
            if (platformSpecificOutputFolder)
            {
                outputMode = OutputPathMode.BinPlatformArchConfiguration;
            }

            return outputMode;
        }
    }
}

