using System;
using System.Xml;
using System.Collections.Generic;
using Protobuild.Services;

namespace Protobuild
{
    public interface IServiceInputGenerator
    {
        XmlNode Generate(XmlDocument doc, string projectName, IEnumerable<Service> services);
    }
}

