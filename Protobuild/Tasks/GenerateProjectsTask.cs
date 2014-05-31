using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Protobuild.Tasks
{
    using Protobuild.Services;

    public class GenerateProjectsTask : BaseTask
    {
        [Required]
        public string SourcePath
        {
            get;
            set;
        }

        [Required]
        public string RootPath
        {
            get;
            set;
        }

        [Required]
        public string Platform
        {
            get;
            set;
        }

        [Required]
        public string ModuleName
        {
            get;
            set;
        }

        [Required]
        public string[] EnableServices { get; set; }

        [Required]
        public string[] DisableServices { get; set; }

        [Required]
        public string ServiceSpecPath { get; set; }

        public override bool Execute()
        {
            if (string.Compare(this.Platform, "Web", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // Trigger JSIL provider download if needed.
                string jsilDirectory, jsilCompilerFile;
                var jsilProvider = new JSILProvider();
                if (!jsilProvider.GetJSIL(out jsilDirectory, out jsilCompilerFile))
                {
                    return false;
                }
            }

            this.LogMessage(
                "Starting generation of projects for " + this.Platform);

            var module = ModuleInfo.Load(Path.Combine(this.RootPath, "Build", "Module.xml"));
            var definitions = module.GetDefinitionsRecursively().ToArray();

            var generator = new ProjectGenerator(
                this.RootPath,
                this.Platform,
                this.LogMessage);
            foreach (var definition in definitions)
            {
                this.LogMessage("Loading: " + definition.Name);
                generator.Load(Path.Combine(
                    definition.ModulePath,
                    "Build",
                    "Projects",
                    definition.Name + ".definition"),
                    module.Path,
                    definition.ModulePath);
            }

            var serviceManager = new ServiceManager(generator);
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

                try
                {
                    services = serviceManager.CalculateDependencyGraph();
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
            foreach (var submodule in module.GetSubmodules())
            {
                this.LogMessage(
                    "Invoking submodule generation for " + submodule.Name);
                submodule.RunProtobuild("-generate " + this.Platform + " -spec " + serviceSpecPath);
                this.LogMessage(
                    "Finished submodule generation for " + submodule.Name);
            }

            var repositoryPaths = new List<string>();

            foreach (var definition in definitions.Where(x => x.ModulePath == module.Path))
            {
                string repositoryPath;
                var definitionCopy = definition;
                generator.Generate(
                    definition.Name,
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
            generator.GenerateSolution(solution, services, repositoryPaths);

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

