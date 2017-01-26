using System;
using System.Xml;
using System.Xml.Xsl;

namespace Protobuild
{
    internal interface IResourceProvider
    {
        XslCompiledTransform LoadXSLT(string workingDirectory, ResourceType resourceType, Language language, string platform);

        XmlDocument LoadXML(string workingDirectory, ResourceType resourceType, Language language, string platform);
    }
}

