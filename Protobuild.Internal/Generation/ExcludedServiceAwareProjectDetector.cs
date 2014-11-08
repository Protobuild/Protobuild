using System;
using System.Xml;
using Protobuild.Services;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    public class ExcludedServiceAwareProjectDetector : IExcludedServiceAwareProjectDetector
    {
        public bool IsExcludedServiceAwareProject(string name, XmlDocument projectDoc, List<Service> services)
        {
            return projectDoc.DocumentElement.ChildNodes.OfType<XmlElement>().Any(x => x.Name == "Services")
                && services.All(x => x.ProjectName != name);
        }
    }
}

