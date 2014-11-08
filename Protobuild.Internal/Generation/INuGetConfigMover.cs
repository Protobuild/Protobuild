using System;
using System.Xml;

namespace Protobuild
{
    public interface INuGetConfigMover
    {
        void Move(string rootPath, string platformName, System.Xml.XmlDocument projectDoc);
    }
}

