using System;
using System.Xml;

namespace Protobuild
{
    internal interface IProjectLoader
    {
        LoadedDefinitionInfo Load(string targetPlatform, ModuleInfo module, DefinitionInfo definition);
    }
}

