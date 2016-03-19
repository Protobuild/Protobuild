using System;
using System.Xml;

namespace Protobuild
{
    internal interface INuGetReferenceDetector
    {
        void ApplyNuGetReferences(
            string rootPath,
            string packagesPath,
            XmlDocument document,
            XmlNode nuget);
    }
}

