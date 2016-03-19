using System;
using System.Collections.Generic;
using System.Xml;
using Protobuild.Services;

namespace Protobuild
{
    internal interface IProjectGenerator
    {
        void Generate(
            DefinitionInfo current,
            List<LoadedDefinitionInfo> definitions,
            string rootPath,
            string projectName,
            string platformName,
            List<Service> services,
            out string packagesFilePath,
            Action onActualGeneration);
    }
}

