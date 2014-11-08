using System;
using System.Xml;
using System.Collections.Generic;
using Protobuild.Services;

namespace Protobuild
{
    public interface ISolutionInputGenerator
    {
        XmlDocument GenerateForSelectSolution(List<XmlDocument> definitions, string platform, List<Service> services);

        XmlDocument GenerateForGenerateSolution(string platform, IEnumerable<XmlElement> projectElements);
    }
}

