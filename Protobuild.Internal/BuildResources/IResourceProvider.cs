using System;
using System.Xml.Xsl;

namespace Protobuild
{
    internal interface IResourceProvider
    {
        XslCompiledTransform LoadXSLT(ResourceType resourceType, Language language, string platform);
    }
}

