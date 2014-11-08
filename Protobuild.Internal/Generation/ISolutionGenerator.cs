using System;
using System.Xml;
using Protobuild.Services;
using System.Collections.Generic;

namespace Protobuild
{
    public interface ISolutionGenerator
    {
        void Generate(
            ModuleInfo moduleInfo,
            List<XmlDocument> definitions,
            string platformName,
            string solutionPath,
            List<Service> services,
            IEnumerable<string> repositoryPaths);
    }
}

