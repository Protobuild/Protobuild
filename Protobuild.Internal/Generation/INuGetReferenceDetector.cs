using System;
using System.Xml;

namespace Protobuild
{
    public interface INuGetReferenceDetector
    {
        void ApplyNuGetReferences(
            string rootPath,
            string packagesPath,
            XmlDocument document,
            XmlNode nuget);
    }
}

