using System;
using System.Xml;

namespace Protobuild
{
    public interface IProjectLoader
    {
        LoadedDefinitionInfo Load(string targetPlatform, ModuleInfo module, DefinitionInfo definition);
    }
}

