using System;
using System.Xml;
using System.Collections.Generic;

namespace Protobuild
{
    public interface IExternalProjectReferenceResolver
    {
        void ResolveExternalProjectReferences(List<LoadedDefinitionInfo> documents, XmlDocument projectDoc, string targetPlatform);
    }
}

