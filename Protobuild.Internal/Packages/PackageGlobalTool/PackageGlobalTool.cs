using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Protobuild
{
    public class PackageGlobalTool : IPackageGlobalTool
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;

        public PackageGlobalTool(IHostPlatformDetector hostPlatformDetector)
        {
            _hostPlatformDetector = hostPlatformDetector;
        }

        private string GetToolsPath()
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var toolsPath = Path.Combine(basePath, ".protobuild-tools");

            if (!Directory.Exists(toolsPath))
            {
                Directory.CreateDirectory(toolsPath);
            }

            return toolsPath;
        }

        public string GetGlobalToolInstallationPath(PackageRef reference)
        {
            var toolsPath = this.GetToolsPath();

            var sha1 = new SHA1Managed();
            var urlHashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(reference.Uri));
            var urlHashString = BitConverter.ToString(urlHashBytes).Replace("-", "").ToLowerInvariant();

            var toolPath = Path.Combine(toolsPath, urlHashString);

            if (!Directory.Exists(toolPath))
            {
                Directory.CreateDirectory(toolPath);
            }

            return toolPath;
        }

        public void ScanPackageForToolsAndInstall(string toolFolder)
        {
            var projectsPath = Path.Combine(toolFolder, "Build", "Projects");
            var projectsInfo = new DirectoryInfo(projectsPath);

            foreach (var file in projectsInfo.GetFiles("*.definition"))
            {
                var document = XDocument.Load(file.FullName);
                var tools = document.XPathSelectElements("/ExternalProject/Tool");

                foreach (var tool in tools)
                {
                    var toolPath = Path.Combine(toolFolder, tool.Attribute(XName.Get("Path")).Value);
                    var toolName = tool.Attribute(XName.Get("Name")).Value;

                    if (Path.DirectorySeparatorChar == '\\')
                    {
                        toolPath = toolPath.Replace("/", "\\");
                    }
                    else if (Path.DirectorySeparatorChar == '/')
                    {
                        toolPath = toolPath.Replace("\\", "/");
                    }

                    using (var writer = new StreamWriter(Path.Combine(this.GetToolsPath(), toolName + ".tool")))
                    {
                        writer.WriteLine(toolPath);
                    }

                    Console.WriteLine("Global tool '" + toolName + "' now points to '" + toolPath + "'");

                    if (_hostPlatformDetector.DetectPlatform() == "Windows")
                    {
                        this.InstallToolIntoWindowsStartMenu(toolName, toolPath);
                    }
                }
            }
        }

        private void InstallToolIntoWindowsStartMenu(string toolName, string toolPath)
        {
            var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                toolName.Replace('.', ' ') + ".url");
            using (var writer = new StreamWriter(startMenuPath, false))
            {
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=file:///" + toolPath);
                writer.WriteLine("IconIndex=0");
                writer.WriteLine("IconFile=" + toolPath.Replace('\\', '/'));
                writer.Flush();
            }
        }

        public string ResolveGlobalToolIfPresent(string toolName)
        {
            var toolsPath = this.GetToolsPath();
            var toolNameFile = Path.Combine(toolsPath, toolName + ".tool");

            if (File.Exists(toolNameFile))
            {
                using (var reader = new StreamReader(toolNameFile))
                {
                    return reader.ReadToEnd().Trim();
                }
            }

            return null;
        }
    }
}

