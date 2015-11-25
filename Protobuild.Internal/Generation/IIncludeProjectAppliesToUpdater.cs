using System;
using System.Collections.Generic;
using System.Xml;

namespace Protobuild
{
    public interface IIncludeProjectAppliesToUpdater
    {
        void UpdateProjectReferences(List<XmlDocument> documents, XmlDocument projectDoc);
    }
}

