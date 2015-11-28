using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Protobuild.Tasks
{
    using Protobuild.Services;

    public class GenerateProjectsTask : BaseTask
    {
        private readonly IProjectLoader m_ProjectLoader;

        private readonly IProjectGenerator m_ProjectGenerator;

        private readonly ISolutionGenerator m_SolutionGenerator;

        private readonly IJSILProvider m_JSILProvider;

        private readonly IPackageRedirector m_PackageRedirector;

        public GenerateProjectsTask(
            IProjectLoader projectLoader,
            IProjectGenerator projectGenerator,
            ISolutionGenerator solutionGenerator,
            IJSILProvider jsilProvider,
            IPackageRedirector packageRedirector)
        {
            this.m_ProjectLoader = projectLoader;
            this.m_ProjectGenerator = projectGenerator;
            this.m_SolutionGenerator = solutionGenerator;
            this.m_JSILProvider = jsilProvider;
            this.m_PackageRedirector = packageRedirector;
        }

        public string SourcePath
        {
            get;
            set;
        }

        public string RootPath
        {
            get;
            set;
        }

        public string Platform
        {
            get;
            set;
        }

        public string ModuleName
        {
            get;
            set;
        }

        public string[] EnableServices { get; set; }

        public string[] DisableServices { get; set; }

        public string ServiceSpecPath { get; set; }

        public bool DebugServiceResolution { get; set; }

        public bool DisablePackageResolution { get; set; }

        public override bool Execute()
        {
            if (string.Compare(this.Platform, "Web", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // Trigger JSIL provider download if needed.
                string jsilDirectory, jsilCompilerFile;
                if (!this.m_JSILProvider.GetJSIL(out jsilDirectory, out jsilCompilerFile))
                {
                    return false;
                }
            }

            var module = ModuleInfo.Load(Path.Combine(this.RootPath, "Build", "Module.xml"));

            this.LogMessage(
                "Starting generation of projects for " + this.Platform);

            var definitions = module.GetDefinitionsRecursively(this.Platform).ToArray();
            var loadedProjects = new List<XmlDocument>();

            foreach (var definition in definitions)
            {
                this.LogMessage("Loading: " + definition.Name);
                loadedProjects.Add(
                    this.m_ProjectLoader.Load(
                        this.Platform,
                        module,
                        definition));
            }

            var serviceManager = new ServiceManager(this.Platform);
            List<Service> services;
            string serviceSpecPath;

            if (this.ServiceSpecPath == null)
            {
                serviceManager.SetRootDefinitions(module.GetDefinitions());

                if (this.EnableServices == null)
                {
                    this.EnableServices = new string[0];
                }

                if (this.DisableServices == null)
                {
                    this.DisableServices = new string[0];
                }

                foreach (var service in this.EnableServices)
                {
                    serviceManager.EnableService(service);
                }

                foreach (var service in this.DisableServices)
                {
                    serviceManager.DisableService(service);
                }

                if (this.DebugServiceResolution)
                {
                    serviceManager.EnableDebugInformation();
                }

                try
                {
                    services = serviceManager.CalculateDependencyGraph(loadedProjects);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine("Error during service resolution: " + ex.Message);
                    return false;
                }

                serviceSpecPath = serviceManager.SaveServiceSpec(services);

                foreach (var service in services)
                {
                    if (service.ServiceName != null)
                    {
                        this.LogMessage("Enabled service: " + service.FullName);
                    }
                }
            }
            else
            {
                services = serviceManager.LoadServiceSpec(this.ServiceSpecPath);
                serviceSpecPath = this.ServiceSpecPath;
            }

            // Run Protobuild in batch mode in each of the submodules
            // where it is present.
            foreach (var submodule in module.GetSubmodules(this.Platform))
            {
                if (submodule.HasProtobuildFeature("skip-invocation-on-no-standard-projects"))
                {
                    if (submodule.GetDefinitionsRecursively(this.Platform).All(x => !x.IsStandardProject))
                    {
                        // Do not invoke this submodule.
                        this.LogMessage(
                            "Skipping submodule generation for " + submodule.Name + " (there are no projects to generate)");
                        continue;
                    }
                }

                this.LogMessage(
                    "Invoking submodule generation for " + submodule.Name);
                var noResolve = submodule.HasProtobuildFeature("no-resolve") ? " -no-resolve" : string.Empty;
                submodule.RunProtobuild(
                    "-generate " + this.Platform + 
                    " -spec " + serviceSpecPath + 
                    " " + this.m_PackageRedirector.GetRedirectionArguments() + 
                    noResolve);
                this.LogMessage(
                    "Finished submodule generation for " + submodule.Name);
            }

            var repositoryPaths = new List<string>();

            foreach (var definition in definitions.Where(x => x.ModulePath == module.Path))
            {
                string repositoryPath;
                var definitionCopy = definition;
                this.m_ProjectGenerator.Generate(
                    definition,
                    loadedProjects,
                    this.RootPath,
                    definition.Name,
                    this.Platform,
                    services,
                    out repositoryPath,
                    () => this.LogMessage("Generating: " + definitionCopy.Name));

                // Only add repository paths if they should be generated.
                if (module.GenerateNuGetRepositories && !string.IsNullOrEmpty(repositoryPath))
                {
                    repositoryPaths.Add(repositoryPath);
                }
            }

            var solution = Path.Combine(
                this.RootPath,
                this.ModuleName + "." + this.Platform + ".sln");
            this.LogMessage("Generating: (solution)");
            this.m_SolutionGenerator.Generate(
                module, 
                loadedProjects,
                this.Platform,
                solution, 
                services, 
                repositoryPaths);

            // Only save the specification cache if we allow synchronisation
            if (module.DisableSynchronisation == null || !module.DisableSynchronisation.Value)
            {
                var serviceCache = Path.Combine(this.RootPath, this.ModuleName + "." + this.Platform + ".speccache");
                this.LogMessage("Saving service specification");
                File.Copy(serviceSpecPath, serviceCache, true);
            }

            this.LogMessage(
                "Generation complete.");
            
            return true;
        }
    }
}

