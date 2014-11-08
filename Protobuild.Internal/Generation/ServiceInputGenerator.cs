using System;
using System.Xml;
using System.Collections.Generic;
using Protobuild.Services;

namespace Protobuild
{
    public class ServiceInputGenerator : IServiceInputGenerator
    {
        public XmlNode Generate(XmlDocument doc, string projectName, IEnumerable<Service> services)
        {
            var servicesElements = doc.CreateElement("Services");
            string activeServiceNames = null;

            foreach (var service in services)
            {
                var serviceElement = doc.CreateElement("Service");
                serviceElement.SetAttribute("Name", service.FullName);
                serviceElement.SetAttribute("Project", service.ProjectName);
                this.AddList(doc, serviceElement, service.AddDefines, "AddDefines", "AddDefine");
                this.AddList(doc, serviceElement, service.RemoveDefines, "RemoveDefines", "RemoveDefine");
                this.AddList(doc, serviceElement, service.AddReferences, "AddReferences", "AddReference");
                servicesElements.AppendChild(serviceElement);

                if (activeServiceNames == null)
                {
                    activeServiceNames = service.FullName;
                }
                else
                {
                    activeServiceNames += "," + service.FullName;
                }

                if (projectName != null)
                {
                    if (!string.IsNullOrEmpty(service.ServiceName))
                    {
                        if (service.ProjectName == projectName)
                        {
                            // Include relative service name in list.
                            activeServiceNames += "," + service.ServiceName;
                        }
                    }
                }
            }

            var activeServicesNamesElement = doc.CreateElement("ActiveServicesNames");
            activeServicesNamesElement.InnerText = activeServiceNames ?? string.Empty;
            servicesElements.AppendChild(activeServicesNamesElement);

            return servicesElements;
        }

        private void AddList(XmlDocument doc, XmlElement serviceElement, IEnumerable<string> entries, string containerName, string entryName)
        {
            var element = doc.CreateElement(containerName);
            serviceElement.AppendChild(element);

            foreach (var entry in entries)
            {
                var entryElement = doc.CreateElement(entryName);
                entryElement.InnerText = entry;
                element.AppendChild(entryElement);
            }
        }
    }
}

