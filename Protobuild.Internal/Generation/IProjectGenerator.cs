using System;
using System.Collections.Generic;
using System.Xml;
using Protobuild.Services;

namespace Protobuild
{
    public interface IProjectGenerator
    {
        void Generate(
            List<XmlDocument> definitions1,
            string rootPath,
            string projectName,
            string platformName,
            List<Service> services,
            out string packagesFilePath,
            Action onActualGeneration);
    }
}

