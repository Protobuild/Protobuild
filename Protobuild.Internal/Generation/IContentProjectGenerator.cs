using System;
using System.Xml;

namespace Protobuild
{
    public interface IContentProjectGenerator
    {
        XmlDocument Generate(string targetPlatform, XmlDocument source, string rootPath);
    }
}

