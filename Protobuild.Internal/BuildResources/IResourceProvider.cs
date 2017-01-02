using System;
using System.Xml;
using System.Xml.Xsl;

namespace Protobuild
{
    internal interface IResourceProvider
    {
        XslCompiledTransform LoadXSLT(ResourceType resourceType, Language language, string platform);

        XmlDocument LoadXML(ResourceType resourceType, Language language, string platform);
    }
}

