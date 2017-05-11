using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Linq;
using System.Runtime.InteropServices;

namespace Protobuild
{
    internal class PackageGlobalTool : IPackageGlobalTool
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

        public string GetGlobalToolInstallationPath(string referenceURI)
        {
            var toolsPath = this.GetToolsPath();

            var sha1 = new SHA1Managed();
            var urlHashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(referenceURI));
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

                    RedirectableConsole.WriteLine("Global tool '" + toolName + "' now points to '" + toolPath + "'");

                    if (_hostPlatformDetector.DetectPlatform() == "Windows")
                    {
                        this.InstallToolIntoWindowsStartMenu(toolName, toolPath);
					}
					else if (_hostPlatformDetector.DetectPlatform() == "MacOS")
					{
						this.InstallToolIntoUserApplicationFolder(toolName, toolPath);
					}
                    else if (_hostPlatformDetector.DetectPlatform() == "Linux")
                    {
                        this.InstallToolIntoLinuxApplicationMenu(toolName, toolPath);
                    }
                }
            }
        }

        private void InstallToolIntoWindowsStartMenu(string toolName, string toolPath)
        {
            var startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                toolName.Replace('.', ' ') + ".lnk");

            string urlStyleLink = Path.ChangeExtension(startMenuPath, ".url");
            if (File.Exists(urlStyleLink))
            {
                // Remove the old (and somewhat buggy) .url link that was produced by older versions of Protobuild
                File.Delete(urlStyleLink);
            }

            Type t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")); //Windows Script Host Shell Object
            dynamic shell = Activator.CreateInstance(t);
            try
            {
                var lnk = shell.CreateShortcut(startMenuPath);
                try
                {
                    lnk.TargetPath = toolPath;
                    lnk.IconLocation = toolPath + ", 0";
                    lnk.Save();
                }
                finally
                {
                    Marshal.FinalReleaseComObject(lnk);
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
		}

		private void InstallToolIntoUserApplicationFolder(string toolName, string toolPath)
		{
			var appToolPath = toolPath.Replace(".exe", ".app");
			if (!Directory.Exists(appToolPath))
			{
				return;
			}

			var basename = Path.GetFileName(appToolPath);
			var installPath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"Applications",
				basename);
			if (File.Exists(installPath))
			{
				File.Delete(installPath);
			}

			var install = System.Diagnostics.Process.Start("ln", "-s '" + appToolPath + "' '" + installPath + "'");
			if (install != null)
			{
				install.WaitForExit();

				RedirectableConsole.WriteLine("Global tool '" + toolName + "' is now available in the application menu");
			}
			else
			{
				RedirectableConsole.WriteLine("Unable to install global tool '" + toolName + "' into the application menu (unable to create link)");
			}
		}

        private void InstallToolIntoLinuxApplicationMenu(string toolName, string toolPath)
        {
            RedirectableConsole.WriteLine("Installing global tool '" + toolName + "' into the application menu...");

            var menuPath = Path.Combine(GetToolsPath(), ".linux-menus");
            Directory.CreateDirectory(menuPath);

            // Extract the icon from the assembly.
            string iconPath = null;
            try
            {
                var asm = System.Reflection.Assembly.Load("System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                var iconType = asm.GetType("System.Drawing.Icon");
                var extractMethod = iconType.GetMethod("ExtractAssociatedIcon", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                dynamic iconObject = extractMethod.Invoke(null, new[] { toolPath });
                using (var bmp = iconObject.ToBitmap())
                {
                    var iconPathTemp = Path.Combine(menuPath, toolName + ".png");
                    var enumType = asm.GetType("System.Drawing.Imaging.ImageFormat");
                    var saveMethod = ((Type)bmp.GetType()).GetMethods().First(x => x.Name == "Save" && x.GetParameters().Length == 2);
                    saveMethod.Invoke(
                        bmp,
                        new object[] {
                            iconPathTemp,
                            enumType.GetProperty("Png").GetGetMethod().Invoke(null, null)
                        });
                    iconPath = iconPathTemp;
                }
            }
            catch (Exception)
            {
                // No icon to extract.
            }

            var menuItemPath = Path.Combine(menuPath, "protobuild-" + toolName + ".desktop");

            using (var writer = new StreamWriter(menuItemPath))
            {
                writer.WriteLine("[Desktop Entry]");
                writer.WriteLine("Type=Application");
                writer.WriteLine("Name=" + toolName);
                if (iconPath != null)
                {
                    writer.WriteLine("Icon=" + iconPath);
                }
                writer.WriteLine("Exec=/usr/bin/mono " + toolPath);
                writer.WriteLine("Categories=Development");
            }

            var install = System.Diagnostics.Process.Start("xdg-desktop-menu", "install '" + menuItemPath + "'");
            if (install != null)
            {
                install.WaitForExit();

                RedirectableConsole.WriteLine("Global tool '" + toolName + "' is now available in the application menu");
            }
            else
            {
                RedirectableConsole.WriteLine("Unable to install global tool '" + toolName + "' into the application menu (xdg-desktop-menu not found)");
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

