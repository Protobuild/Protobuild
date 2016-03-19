using System;
using System.Xml.Linq;

namespace Protobuild
{
    internal interface IProjectOutputPathCalculator
    {
        string GetProjectOutputPathPrefix(string platform, DefinitionInfo definition, XDocument document, bool asRegex);

        string GetProjectAssemblyName(string platform, DefinitionInfo definition, XDocument document);

        OutputPathMode GetProjectOutputMode(XDocument document);
    }
}

