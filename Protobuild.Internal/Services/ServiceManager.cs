namespace Protobuild.Services
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
    internal class ServiceManager
    {
        private const int SERIALIZATION_VERSION = 3;

        private readonly string m_Platform;

        private readonly List<string> m_EnabledServices;

        private readonly List<string> m_DisabledServices;

        private DefinitionInfo[] m_RootDefinitions;

        private bool m_ShowDebugInformation;

        public ServiceManager(string platform)
        {
            this.m_Platform = platform;
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

        public void EnableDebugInformation()
        {
            this.m_ShowDebugInformation = true;
        }

        public List<Service> CalculateDependencyGraph(List<XmlDocument> definitions)
        {
            var services = this.LoadServices(definitions);

            this.CalculateServices(services);

            this.EnableReferencedProjects(definitions, services);

            this.EnableRootProjects(services);

            this.EnableDefaultAndExplicitServices(services);

            return this.ResolveServices(services);
        }

        private void EnableRootProjects(List<Service> services)
        {
            if (this.m_ShowDebugInformation)
            {
                Console.WriteLine("Enabling project root services:");
            }

            foreach (var definition in this.m_RootDefinitions)
            {
                var defaultService =
                    services.FirstOrDefault(
                        x => x.ProjectName == definition.Name && string.IsNullOrEmpty(x.ServiceName));

                if (defaultService != null)
                {
                    if (this.m_ShowDebugInformation)
                    {
                        Console.WriteLine(defaultService.FullName + " set to Default, because it is a project root service");
                    }

                    defaultService.DesiredLevel = ServiceDesiredLevel.Default;
                }
            }
        }

        private void EnableReferencedProjects(List<XmlDocument> definitions, List<Service> services)
        {
            if (this.m_ShowDebugInformation)
            {
                Console.WriteLine("Enabling services in explicitly referenced projects:");
            }

            var lookup = services.ToDictionarySafe(
                k => k.FullName,
                v => v,
                (dict, d) => Console.WriteLine("WARNING: There is more than one service with the full name " + d.FullName));

            var references = from definition in definitions
                             where definition.DocumentElement.Name == "Project"
                             select
                                 definition.DocumentElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name == "References")
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
                if (this.m_ShowDebugInformation)
                {
                    Console.WriteLine(lookup[reference].FullName + " set to Required, because there is an explicit reference to the project");
                }

                lookup[reference].DesiredLevel = ServiceDesiredLevel.Required;
            }
        }

        internal void EnableDefaultAndExplicitServices(List<Service> services)
        {
            if (this.m_ShowDebugInformation)
            {
                Console.WriteLine("Enabling default services, and services enabled / disabled by the user:");
            }

            foreach (var service in services)
            {
                if (service.DefaultForRoot)
                {
                    if (this.m_RootDefinitions.Any(x => x.Name == service.ProjectName))
                    {
                        if (this.m_ShowDebugInformation)
                        {
                            Console.WriteLine(service.FullName + " set to Default, because it is marked DefaultForRoot");
                        }

                        service.DesiredLevel = ServiceDesiredLevel.Default;
                    }
                }

                if (this.m_EnabledServices.Any(x => x == service.ServiceName || x == service.FullName))
                {
                    if (this.m_ShowDebugInformation)
                    {
                        Console.WriteLine(service.FullName + " set to Required, because it is explicitly enabled by the user");
                    }

                    service.DesiredLevel = ServiceDesiredLevel.Required;
                }

                if (this.m_DisabledServices.Any(x => x == service.ServiceName || x == service.FullName))
                {
                    if (this.m_ShowDebugInformation)
                    {
                        Console.WriteLine(service.FullName + " set to Disabled, because it is explicitly disabled by the user");
                    }

                    service.DesiredLevel = ServiceDesiredLevel.Disabled;
                }
            }
        }

        internal List<Service> ResolveServices(List<Service> services)
        {
            var pass = 1;
            if (this.m_ShowDebugInformation)
            {
                Console.WriteLine("Performing service resolution pass " + pass + ":");
            }

            while (this.PerformResolutionPass(services))
            {
                // Returns true if it made any modifications.

                ++pass;

                if (this.m_ShowDebugInformation)
                {
                    Console.WriteLine("Performing service resolution pass " + pass + ":");
                }
            }

            if (this.m_ShowDebugInformation)
            {
                Console.WriteLine("Service resolution passes complete.");
            }

            return services.Where(x => x.DesiredLevel != ServiceDesiredLevel.Disabled && x.DesiredLevel != ServiceDesiredLevel.Unused).ToList();
        }

        private bool PerformResolutionPass(List<Service> services)
        {
            var lookup = services.ToDictionarySafe(
                k => k.FullName,
                v => v,
                (dict, d) => { /* We have already warned about this previously */ });
            var modified = false;

            foreach (var service in services)
            {
                if (service.DesiredLevel == ServiceDesiredLevel.Disabled || service.DesiredLevel == ServiceDesiredLevel.Unused)
                {
                    continue;
                }

                foreach (var require in service.Requires)
                {
                    if (!lookup.ContainsKey(require))
                    {
                        throw new InvalidOperationException(
                            service.FullName + " requires " + require + ", but it does not exist.");
                    }

                    if (lookup[require].DesiredLevel == ServiceDesiredLevel.Disabled)
                    {
                        if (service.DesiredLevel == ServiceDesiredLevel.Required || service.ServiceName == null)
                        {
                            throw new InvalidOperationException(
                                service.FullName + " requires " + require + ", but you have explicitly requested it be disabled.");
                        }
                        else if (service.DesiredLevel == ServiceDesiredLevel.Recommended ||
                            service.DesiredLevel == ServiceDesiredLevel.Default)
                        {
                            if (this.m_ShowDebugInformation)
                            {
                                Console.WriteLine(service.FullName + " set to Disabled, because it's dependency " + lookup[require].FullName + " is set to Disabled");
                            }

                            service.DesiredLevel = ServiceDesiredLevel.Disabled;
                            modified = true;
                        }
                    }
                    else if (service.DesiredLevel == ServiceDesiredLevel.Required)
                    {
                        if (lookup[require].DesiredLevel != ServiceDesiredLevel.Required)
                        {
                            if (this.m_ShowDebugInformation)
                            {
                                Console.WriteLine(lookup[require].FullName + " set to Required, because it's dependency " + service.FullName + " is set to Required");
                            }

                            lookup[require].DesiredLevel = ServiceDesiredLevel.Required;
                            modified = true;
                        }
                    }
                    else if (service.DesiredLevel == ServiceDesiredLevel.Recommended)
                    {
                        if (lookup[require].DesiredLevel != ServiceDesiredLevel.Required &&
                            lookup[require].DesiredLevel != ServiceDesiredLevel.Recommended)
                        {
                            if (this.m_ShowDebugInformation)
                            {
                                Console.WriteLine(lookup[require].FullName + " set to Recommended, because it's dependency " + service.FullName + " is set to Recommended");
                            }

                            lookup[require].DesiredLevel = ServiceDesiredLevel.Recommended;
                            modified = true;
                        }
                    }
                    else if (service.DesiredLevel == ServiceDesiredLevel.Default)
                    {
                        if (lookup[require].DesiredLevel != ServiceDesiredLevel.Required &&
                            lookup[require].DesiredLevel != ServiceDesiredLevel.Recommended &&
                            lookup[require].DesiredLevel != ServiceDesiredLevel.Default)
                        {
                            if (this.m_ShowDebugInformation)
                            {
                                Console.WriteLine(lookup[require].FullName + " set to Default, because it's dependency " + service.FullName + " is set to Default");
                            }

                            lookup[require].DesiredLevel = ServiceDesiredLevel.Default;
                            modified = true;
                        }
                    }
                }

                foreach (var recommend in service.Recommends)
                {
                    if (!lookup.ContainsKey(recommend))
                    {
                        throw new InvalidOperationException(
                            service.FullName + " recommends " + recommend + ", but it does not exist.");
                    }

                    if (lookup[recommend].DesiredLevel != ServiceDesiredLevel.Disabled &&
                        lookup[recommend].DesiredLevel != ServiceDesiredLevel.Recommended &&
                        lookup[recommend].DesiredLevel != ServiceDesiredLevel.Required)
                    {
                        if (this.m_ShowDebugInformation)
                        {
                            Console.WriteLine(lookup[recommend].FullName + " set to Recommended, because it's dependency " + service.FullName + " recommends it");
                        }

                        lookup[recommend].DesiredLevel = ServiceDesiredLevel.Recommended;
                        modified = true;
                    }
                }

                foreach (var conflict in service.Conflicts)
                {
                    if (!lookup.ContainsKey(conflict))
                    {
                        // The service that the conflict is declared with may
                        // not exist because it's not available on the current
                        // platform, in which case it can't be enabled anyway.
                        continue;
                    }

                    if (lookup[conflict].DesiredLevel == ServiceDesiredLevel.Required &&
                        service.DesiredLevel == ServiceDesiredLevel.Required)
                    {
                        throw new InvalidOperationException(
                            service.FullName + " conflicts with " + lookup[conflict].FullName + ", but both are enabled (and required).");
                    }

                    if (lookup[conflict].DesiredLevel == ServiceDesiredLevel.Recommended)
                    {
                        if (this.m_ShowDebugInformation)
                        {
                            Console.WriteLine(lookup[conflict].FullName + " set to Disabled, because " + service.FullName + " conflicts with it");
                        }

                        // The service this conflicts with is only recommended, so we can
                        // safely disable it.
                        lookup[conflict].DesiredLevel = ServiceDesiredLevel.Disabled;
                        modified = true;
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
                var removeDefine = SelectElementsFromService(service, "RemoveDefine");
                var reference = SelectElementsFromService(service, "Reference");
                var defaultForRoot = SelectElementsFromService(service, "DefaultForRoot").FirstOrDefault();
                var requires = SelectElementsFromService(service, "Requires");
                var recommends = SelectElementsFromService(service, "Recommends");
                var conflicts = SelectElementsFromService(service, "Conflicts");
                var infersReference = SelectElementsFromService(service, "InfersReference").FirstOrDefault();

                service.AddDefines.AddRange(addDefine.SelectMany(x => x.InnerText.Split(new[] { ',', ';' })));
                service.RemoveDefines.AddRange(removeDefine.SelectMany(x => x.InnerText.Split(new[] { ',', ';' })));
                service.AddReferences.AddRange(reference.Select(x => x.GetAttribute("Include")));
                service.DefaultForRoot = defaultForRoot != null && string.Equals(defaultForRoot.InnerText, "True", StringComparison.InvariantCultureIgnoreCase);
                service.Requires.AddRange(requires.SelectMany(x => x.InnerText.Split(',')).Select(x => this.AbsolutizeServiceReference(service.ProjectName, x)));
                service.Recommends.AddRange(recommends.SelectMany(x => x.InnerText.Split(',')).Select(x => this.AbsolutizeServiceReference(service.ProjectName, x)));
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
                var defaultService = new Service
                {
                    ProjectName = doc.DocumentElement.GetAttribute("Name"),
                    DesiredLevel = ServiceDesiredLevel.Unused
                };
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
                                    Requires = { defaultService.FullName },
                                    DesiredLevel = ServiceDesiredLevel.Unused
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

                    foreach (var usage in declaredDependencies.ChildNodes.OfType<XmlElement>().Where(x => x.Name == "Recommends"))
                    {
                        if (this.ContainsActivePlatform(usage))
                        {
                            defaultService.Recommends.Add(
                                this.AbsolutizeServiceReference(
                                    doc.DocumentElement.GetAttribute("Name"),
                                    usage.GetAttribute("Name")));
                        }
                    }
                }
            }

            return services;
        }

        private bool ContainsActivePlatform(XmlElement xmlElement)
        {
            var platform = xmlElement.ChildNodes.OfType<XmlElement>().FirstOrDefault(x => x.Name == "Platforms");
            if (platform == null)
            {
                return true;
            }

            if (platform.InnerText.Split(',').Contains(this.m_Platform, StringComparer.InvariantCultureIgnoreCase))
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
                    var removeDefines = this.ReadList(reader);
                    var addReferences = this.ReadList(reader);
                    var defaultForRoot = reader.ReadBoolean();
                    var requires = this.ReadList(reader);
                    var recommends = this.ReadList(reader);
                    var conflicts = this.ReadList(reader);
                    var infersReference = reader.ReadBoolean();

                    services.Add(new Service
                    {
                        ProjectName = projectName,
                        ServiceName = serviceName,
                        AddDefines = addDefines,
                        RemoveDefines = removeDefines,
                        AddReferences = addReferences,
                        DefaultForRoot = defaultForRoot,
                        Requires = requires,
                        Recommends = recommends,
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

        public TemporaryServiceSpec SaveServiceSpec(List<Service> services)
        {
            string path;
            try
            {
                path = Path.GetTempFileName();
            }
            catch (IOException)
            {
                // On Windows, if there's more than 65536 files in the
                // temporary directory, then this throws an exception.
                // Instead, create our own path in the temporary
                // directory.
                path = Path.Combine(Path.GetTempPath(), "service." + System.Diagnostics.Process.GetCurrentProcess().Id + ".spec");

                try
                {
                    var stream = File.Create(path);
                    stream.Dispose();
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save service specification to " + path + 
                        ".  This is most likely caused by a full temporary directory.", ex);
                }
            }

            try
            {
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
                        this.WriteList(writer, service.RemoveDefines);
                        this.WriteList(writer, service.AddReferences);
                        writer.Write(service.DefaultForRoot);
                        this.WriteList(writer, service.Requires);
                        this.WriteList(writer, service.Recommends);
                        this.WriteList(writer, service.Conflicts);
                        writer.Write(service.InfersReference);
                    }
                }

                return new TemporaryServiceSpec(path);
            }
            catch
            {
                File.Delete(path);
                throw;
            }
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
