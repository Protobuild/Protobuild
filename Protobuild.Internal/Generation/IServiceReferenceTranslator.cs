using System;
using System.Xml;
using System.Collections.Generic;
using Protobuild.Services;

namespace Protobuild
{
    public interface IServiceReferenceTranslator
    {
        XmlNode TranslateProjectWithServiceReferences(XmlNode importNode, List<Service> services);
    }
}

