﻿namespace Protobuild.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using Protobuild.Tasks;

    /// <summary>
    /// The service manager.
    /// </summary>
    public class ServiceManager
    {
        private const int SERIALIZATION_VERSION = 1;

        private readonly ProjectGenerator m_Generator;

        private readonly List<string> m_EnabledServices;

        private readonly List<string> m_DisabledServices;

        private DefinitionInfo[] m_RootDefinitions;

        public ServiceManager(ProjectGenerator generator)
        {
            this.m_Generator = generator;
            this.m_EnabledServices = new List<string>();
            this.m_DisabledServices = new List<string>();
            this.m_RootDefinitions = new DefinitionInfo[0];
        }

        public void EnableService(string service)
        {
            this.m_EnabledServices.Add(service);
        }

        public void DisableService(string service)
        {
            this.m_DisabledServices.Add(service);
        }

        public void SetRootDefinitions(DefinitionInfo[] definitions)
        {
            this.m_RootDefinitions = definitions;
        }

        public List<Service> CalculateDependencyGraph()
        {
            var services = this.LoadServices(this.m_Generator.Documents);

            this.CalculateServices(services);

            this.EnableReferencedProjects(services);

            this.EnableRootProjects(services);

            this.EnableDefaultAndExplicitServices(services);

            return this.ResolveServices(services);
        }

        private void EnableRootProjects(List<Service> services)
        {
            foreach (var definition in this.m_RootDefinitions)
            {
                var defaultService =
                    services.FirstOrDefault(
                        x => x.ProjectName == definition.Name && string.IsNullOrEmpty(x.ServiceName));

                if (defaultService != null)
                {
                    defaultService.IsEnabled = true;
                }
            }
        }

        private void EnableReferencedProjects(List<Service> services)
        {
            var lookup = services.ToDictionary(k => k.FullName, v => v);

            var references = from definition in this.m_Generator.Documents
                             where definition.Name == "Project"
                             select
                                 definition.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name == "References")
                             into referencesElement
                             where referencesElement != null
                             select
                                 referencesElement.ChildNodes.OfType<XmlElement>()
                                 .Where(x => x.Name == "Reference")
                                 .Select(x => x.GetAttribute("Include"))
                                 .ToList()
                             into referencesList
                             from reference in referencesList.Where(lookup.ContainsKey)
                             select reference;

            foreach (var reference in references)
            {
                lookup[reference].IsEnabled = true;
            }
        }

        private void EnableDefaultAndExplicitServices(List<Service> services)
        {
            foreach (var service in services)
            {
                if (service.DefaultForRoot)
                {
                    if (this.m_RootDefinitions.Any(x => x.Name == service.ProjectName))
                    {
                        service.IsEnabled = true;
                    }
                }

                if (this.m_EnabledServices.Any(x => x == service.ServiceName || x == service.FullName))
                {
                    service.IsEnabled = true;
                }

                if (this.m_DisabledServices.Any(x => x == service.ServiceName || x == service.FullName))
                {
                    service.IsEnabled = false;
                }
            }
        }

        private List<Service> ResolveServices(List<Service> services)
        {
            while (this.PerformResolutionPass(services))
            {
                // Returns true if it made any modifications.
            }

            return services.Where(x => x.IsEnabled).ToList();
        }

        private bool PerformResolutionPass(List<Service> services)
        {
            var lookup = services.ToDictionary(k => k.FullName, v => v);
            var modified = false;

            foreach (var service in services)
            {
                if (!service.IsEnabled)
                {
                    continue;
                }

                foreach (var require in service.Requires)
                {
                    if (!lookup[require].IsEnabled)
                    {
                        lookup[require].IsEnabled = true;
                        modified = true;
                    }
                }

                foreach (var conflict in service.Conflicts)
                {
                    if (lookup[conflict].IsEnabled)
                    {
                        throw new InvalidOperationException(
                            service.FullName + " conflicts with " + lookup[conflict].FullName + ", but both are enabled.");
                    }
                }
            }

            return modified;
        }

        private void CalculateServices(List<Service> services)
        {
            foreach (var service in services)
            {
                if (service.Declaration == null)
                {
                    continue;
                }

                var addDefine = SelectElementsFromService(service, "AddDefine");
                var reference = SelectElementsFromService(service, "Reference");
                var defaultForRoot = SelectElementsFromService(service, "DefaultForRoot").FirstOrDefault();
                var requires = SelectElementsFromService(service, "Requires");
                var conflicts = SelectElementsFromService(service, "Conflicts");
                var infersReference = SelectElementsFromService(service, "InfersReference").FirstOrDefault();

                service.AddDefines.AddRange(addDefine.SelectMany(x => x.InnerText.Split(',')));
                service.AddReferences.AddRange(reference.Select(x => x.GetAttribute("Include")));
                service.DefaultForRoot = defaultForRoot != null && string.Equals(defaultForRoot.InnerText, "True", StringComparison.InvariantCultureIgnoreCase);
                service.Requires.AddRange(requires.SelectMany(x => x.InnerText.Split(',')).Select(x => this.AbsolutizeServiceReference(service.ProjectName, x)));
                service.Conflicts.AddRange(conflicts.SelectMany(x => x.InnerText.Split(',')).Select(x => this.AbsolutizeServiceReference(service.ProjectName, x)));
                service.InfersReference = infersReference == null || string.Equals(infersReference.InnerText, "True", StringComparison.InvariantCultureIgnoreCase);
            }
        }

        private string AbsolutizeServiceReference(string project, string serviceRef)
        {
            if (serviceRef.Contains("/"))
            {
                return serviceRef;
            }

            return project + "/" + serviceRef;
        }

        private static List<XmlElement> SelectElementsFromService(Service service, string name)
        {
            return service.Declaration.ChildNodes.OfType<XmlElement>()
                .Where(x => string.Equals(x.Name, name))
                .ToList();
        }

        private List<Service> LoadServices(List<XmlDocument> documents)
        {
            var services = new List<Service>();

            foreach (var doc in documents)
            {
                if (doc.DocumentElement == null)
                {
                    continue;
                }

                // Add project default service.  This is used to enable service-aware projects
                // based on <Reference> tags.
                var defaultService = new Service { ProjectName = doc.DocumentElement.GetAttribute("Name") };
                services.Add(defaultService);

                var declaredServices =
                    doc.DocumentElement.ChildNodes.OfType<XmlElement>()
                        .FirstOrDefault(
                            x => string.Equals(x.Name, "Services", StringComparison.InvariantCultureIgnoreCase));

                if (declaredServices != null)
                {
                    services.AddRange(
                        declaredServices.ChildNodes.OfType<XmlElement>()
                            .Where(x => string.Equals(x.Name, "Service", StringComparison.InvariantCultureIgnoreCase))
                            .Where(this.ContainsActivePlatform)
                            .Select(
                                serviceElement =>
                                new Service
                                {
                                    Declaration = serviceElement,
                                    ProjectName = doc.DocumentElement.GetAttribute("Name"),
                                    ServiceName = serviceElement.GetAttribute("Name"),
                                    Requires = { defaultService.FullName }
                                }));
                }

                var declaredDependencies =
                    doc.DocumentElement.ChildNodes.OfType<XmlElement>()
                        .FirstOrDefault(
                            x => string.Equals(x.Name, "Dependencies", StringComparison.InvariantCultureIgnoreCase));

                if (declaredDependencies != null)
                {
                    foreach (var usage in declaredDependencies.ChildNodes.OfType<XmlElement>().Where(x => x.Name == "Uses"))
                    {
                        if (this.ContainsActivePlatform(usage))
                        {
                            defaultService.Requires.Add(
                                this.AbsolutizeServiceReference(
                                    doc.DocumentElement.GetAttribute("Name"),
                                    usage.GetAttribute("Name")));
                        }
                    }
                }
            }

            // Filter duplicates and return it as list.
            return services.GroupBy(s => s.FullName)
                .Select(g => g.First ())
                .ToList();
        }

        private bool ContainsActivePlatform(XmlElement xmlElement)
        {
            var platform = xmlElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name == "Platforms");
            if (platform == null)
            {
                return true;
            }

            if (platform.InnerText.Split(',').Contains(this.m_Generator.Platform, StringComparer.InvariantCultureIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public List<Service> LoadServiceSpec(string serviceSpecPath)
        {
            var services = new List<Service>();

            using (var reader = new BinaryReader(new FileStream(serviceSpecPath, FileMode.Open, FileAccess.Read)))
            {
                var version = reader.ReadInt32();

                if (version != SERIALIZATION_VERSION)
                {
                    throw new InvalidOperationException(
                        "The calling version of Protobuild has a different serialization version for the service "
                        + "specification.  Upgrade Protobuild to the latest version.");
                }

                var count = reader.ReadInt32();

                for (var i = 0; i < count; i++)
                {
                    var projectName = reader.ReadString();
                    var serviceName = reader.ReadBoolean() ? reader.ReadString() : null;
                    var addDefines = this.ReadList(reader);
                    var addReferences = this.ReadList(reader);
                    var defaultForRoot = reader.ReadBoolean();
                    var requires = this.ReadList(reader);
                    var conflicts = this.ReadList(reader);
                    var infersReference = reader.ReadBoolean();

                    services.Add(new Service
                    {
                        ProjectName = projectName,
                        ServiceName = serviceName,
                        AddDefines = addDefines,
                        AddReferences = addReferences,
                        DefaultForRoot = defaultForRoot,
                        Requires = requires,
                        Conflicts = conflicts,
                        InfersReference = infersReference
                    });
                }
            }

            return services;
        }

        private List<string> ReadList(BinaryReader reader)
        {
            var count = reader.ReadInt32();
            var list = new List<string>();

            for (var i = 0; i < count; i++)
            {
                list.Add(reader.ReadString());
            }

            return list;
        }

        public string SaveServiceSpec(List<Service> services)
        {
            var path = Path.GetTempFileName();

            using (var writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write)))
            {
                writer.Write(SERIALIZATION_VERSION);

                writer.Write(services.Count);

                foreach (var service in services)
                {
                    writer.Write(service.ProjectName);
                    writer.Write(service.ServiceName != null);
                    if (service.ServiceName != null)
                    {
                        writer.Write(service.ServiceName);
                    }
                    this.WriteList(writer, service.AddDefines);
                    this.WriteList(writer, service.AddReferences);
                    writer.Write(service.DefaultForRoot);
                    this.WriteList(writer, service.Requires);
                    this.WriteList(writer, service.Conflicts);
                    writer.Write(service.InfersReference);
                }
            }

            return path;
        }

        private void WriteList(BinaryWriter writer, List<string> list)
        {
            writer.Write(list.Count);

            foreach (var e in list)
            {
                writer.Write(e);
            }
        }
    }
}