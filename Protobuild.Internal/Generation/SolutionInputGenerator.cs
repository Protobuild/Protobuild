using System;
using System.Collections.Generic;
using Protobuild.Services;
using System.Xml;

namespace Protobuild
{
    internal class SolutionInputGenerator : ISolutionInputGenerator
    {
        private readonly IServiceInputGenerator m_ServiceInputGenerator;

        private readonly IExcludedServiceAwareProjectDetector m_ExcludedServiceAwareProjectDetector;

        private readonly IHostPlatformDetector _hostPlatformDetector;

        private readonly IFeatureManager _featureManager;

        public SolutionInputGenerator(
            IServiceInputGenerator serviceInputGenerator,
            IExcludedServiceAwareProjectDetector excludedServiceAwareProjectDetector,
            IHostPlatformDetector hostPlatformDetector,
            IFeatureManager featureManager)
        {
            this.m_ServiceInputGenerator = serviceInputGenerator;
            this.m_ExcludedServiceAwareProjectDetector = excludedServiceAwareProjectDetector;
            _hostPlatformDetector = hostPlatformDetector;
            _featureManager = featureManager;
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
            var hostPlatformName = doc.CreateElement("HostPlatform");
            hostPlatformName.AppendChild(doc.CreateTextNode(_hostPlatformDetector.DetectPlatform()));
            generation.AppendChild(platformName);
            generation.AppendChild(hostPlatformName);
            input.AppendChild(generation);

            var featuresNode = doc.CreateElement("Features");
            foreach (var feature in _featureManager.GetAllEnabledFeatures())
            {
                var featureNode = doc.CreateElement(feature.ToString());
                featureNode.AppendChild(doc.CreateTextNode("True"));
                featuresNode.AppendChild(featureNode);
            }
            input.AppendChild(featuresNode);

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
            var hostPlatformName = doc.CreateElement("HostPlatform");
            hostPlatformName.AppendChild(doc.CreateTextNode(_hostPlatformDetector.DetectPlatform()));
            generation.AppendChild(platformName);
            generation.AppendChild(hostPlatformName);
            input.AppendChild(generation);

            var featuresNode = doc.CreateElement("Features");
            foreach (var feature in _featureManager.GetAllEnabledFeatures())
            {
                var featureNode = doc.CreateElement(feature.ToString());
                featureNode.AppendChild(doc.CreateTextNode("True"));
                featuresNode.AppendChild(featureNode);
            }
            input.AppendChild(featuresNode);

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

