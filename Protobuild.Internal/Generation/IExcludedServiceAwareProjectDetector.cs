using System;
using System.Xml;
using System.Collections.Generic;
using Protobuild.Services;

namespace Protobuild
{
    public interface IExcludedServiceAwareProjectDetector
    {
        bool IsExcludedServiceAwareProject(string name, XmlDocument projectDoc, List<Service> services);
    }
}

