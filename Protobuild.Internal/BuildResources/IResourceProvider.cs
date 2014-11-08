using System;
using System.Xml.Xsl;

namespace Protobuild
{
    public interface IResourceProvider
    {
        XslCompiledTransform LoadXSLT(ResourceType resourceType, Language language);
    }
}

