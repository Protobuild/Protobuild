using System;
using System.Xml;
using System.Collections.Generic;

namespace Protobuild
{
    public interface IExternalProjectReferenceResolver
    {
        void ResolveExternalProjectReferences(List<XmlDocument> documents, XmlDocument projectDoc);
    }
}

