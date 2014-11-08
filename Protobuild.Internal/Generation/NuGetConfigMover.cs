using System;
using System.IO;

namespace Protobuild
{
    public class NuGetConfigMover : INuGetConfigMover
    {
        public void Move(string rootPath, string platformName, System.Xml.XmlDocument projectDoc)
        {
            var srcPath = Path.Combine(
                rootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar),
                "packages." + platformName + ".config");
            var destPath = Path.Combine(
                rootPath,
                projectDoc.DocumentElement.Attributes["Path"].Value
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar),
                "packages.config");

            if (File.Exists(srcPath))
            {
                File.Copy(srcPath, destPath, true);
            }
        }
    }
}

