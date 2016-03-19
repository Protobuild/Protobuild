using System;
using System.Xml;

namespace Protobuild
{
    internal interface IContentProjectGenerator
    {
        XmlDocument Generate(string targetPlatform, XmlDocument source, string rootPath);
    }
}

