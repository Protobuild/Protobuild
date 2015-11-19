using System;
using System.Xml;

namespace Protobuild
{
    public interface IProjectLoader
    {
        XmlDocument Load(string targetPlatform, ModuleInfo module, DefinitionInfo definition);
    }
}

