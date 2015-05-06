using System;
using System.Xml.Linq;

namespace Protobuild
{
    public interface IProjectOutputPathCalculator
    {
        string GetProjectOutputPathPrefix(string platform, DefinitionInfo definition, XDocument document, bool asRegex);

        string GetProjectAssemblyName(string platform, DefinitionInfo definition, XDocument document);

        OutputPathMode GetProjectOutputMode(XDocument document);
    }
}

