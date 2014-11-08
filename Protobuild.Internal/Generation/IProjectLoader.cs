using System;
using System.Xml;

namespace Protobuild
{
    public interface IProjectLoader
    {
        XmlDocument Load(string path, string platformName, string rootPath, string modulePath);
    }
}

