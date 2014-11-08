using System;
using System.Collections.Generic;
using Protobuild.Services;
using System.Xml;

namespace Protobuild
{
    public class SolutionInputGenerator : ISolutionInputGenerator
    {
        private readonly IServiceInputGenerator m_ServiceInputGenerator;

        private readonly IExcludedServiceAwareProjectDetector m_ExcludedServiceAwareProjectDetector;

        public SolutionInputGenerator(
            IServiceInputGenerator serviceInputGenerator,
            IExcludedServiceAwareProjectDetector excludedServiceAwareProjectDetector)
        {
            this.m_ServiceInputGenerator = serviceInputGenerator;
            this.m_ExcludedServiceAwareProjectDetector = excludedServiceAwareProjectDetector;
        }

        public XmlDocument GenerateForSelectSolution(List<XmlDocument> definitions, string platform, List<Service> services)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var input = doc.CreateElement("Input");
            doc.AppendChild(input);

            input.AppendChild(this.m_ServiceInputGenerator.Generate(doc, null, services));

            var generation = doc.CreateElement("Generation");
            var platformName = doc.CreateElement("Platform");
            platformName.AppendChild(doc.CreateTextNode(platform));
            generation.AppendChild(platformName);
            input.AppendChild(generation);

            var projects = doc.CreateElement("Projects");
            input.AppendChild(projects);
            foreach (var projectDoc in definitions)
            {
                if (this.m_ExcludedServiceAwareProjectDetector.IsExcludedServiceAwareProject(
                    projectDoc.DocumentElement.GetAttribute("Name"),
                    projectDoc,
                    services))
                {
                    continue;
                }

                projects.AppendChild(doc.ImportNode(
                    projectDoc.DocumentElement,
                    true));
            }
            return doc;
        }

        public XmlDocument GenerateForGenerateSolution(string platform, IEnumerable<XmlElement> projectElements)
        {
            var doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "UTF-8", null));
            var input = doc.CreateElement("Input");
            doc.AppendChild(input);

            var generation = doc.CreateElement("Generation");
            var platformName = doc.CreateElement("Platform");
            platformName.AppendChild(doc.CreateTextNode(platform));
            generation.AppendChild(platformName);
            input.AppendChild(generation);

            var projects = doc.CreateElement("Projects");
            input.AppendChild(projects);

            foreach (var projectElem in projectElements)
            {
                projects.AppendChild(doc.ImportNode(projectElem, true));
            }

            return doc;
        }
    }
}

