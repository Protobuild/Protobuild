using System;
using System.Xml;

namespace Protobuild
{
    public interface IContentProjectGenerator
    {
        XmlDocument Generate(string platformName, XmlDocument source, string rootPath);
    }
}

