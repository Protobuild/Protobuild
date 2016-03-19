using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Protobuild.Services;

namespace Protobuild.Tasks
{
    internal class GenerateProjectsTask : BaseTask
    {
        private readonly IJSILProvider m_JSILProvider;

        private readonly IPackageRedirector m_PackageRedirector;

        private readonly IProjectGenerator m_ProjectGenerator;
        private readonly IProjectLoader m_ProjectLoader;

        private readonly ISolutionGenerator m_SolutionGenerator;

        private readonly IModuleExecution _moduleExecution;

        private readonly IFeatureManager _featureManager;

        public GenerateProjectsTask(
            IProjectLoader projectLoader,
            IProjectGenerator projectGenerator,
            ISolutionGenerator solutionGenerator,
            IJSILProvider jsilProvider,
            IPackageRedirector packageRedirector,
            IModuleExecution moduleExecution,
            IFeatureManager featureManager)
        {
            m_ProjectLoader = projectLoader;
            m_ProjectGenerator = projectGenerator;
            m_SolutionGenerator = solutionGenerator;
            m_JSILProvider = jsilProvider;
            m_PackageRedirector = packageRedirector;
            _moduleExecution = moduleExecution;
            _featureManager = featureManager;
        }

        public string SourcePath { get; set; }

        public string RootPath { get; set; }

        public string Platform { get; set; }

        public string ModuleName { get; set; }

        public string[] EnableServices { get; set; }

        public string[] DisableServices { get; set; }

        public string ServiceSpecPath { get; set; }

        public bool DebugServiceResolution { get; set; }

        public bool DisablePackageResolution { get; set; }

        public bool DisableHostPlatformGeneration { get; set; }

        public Action RequiresHostPlatform { get; set; }

        public override bool Execute()
        {
            if (string.Compare(Platform, "Web", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // Trigger JSIL provider download if needed.
                string jsilDirectory, jsilCompilerFile;
                if (!m_JSILProvider.GetJSIL(out jsilDirectory, out jsilCompilerFile))
                {
                    return false;
                }
            }

            var module = ModuleInfo.Load(Path.Combine(RootPath, "Build", "Module.xml"));

            LogMessage(
                "Starting generation of projects for " + Platform);

            var definitions = module.GetDefinitionsRecursively(Platform).ToArray();
            var loadedProjects = new List<LoadedDefinitionInfo>();

            foreach (var definition in definitions)
            {
                LogMessage("Loading: " + definition.Name);
                loadedProjects.Add(
                    m_ProjectLoader.Load(
                        Platform,
                        module,
                        definition));
            }

            var serviceManager = new ServiceManager(Platform);
            List<Service> services;
            TemporaryServiceSpec serviceSpecPath;

            if (ServiceSpecPath == null)
            {
                serviceManager.SetRootDefinitions(module.GetDefinitions());

                if (EnableServices == null)
                {
                    EnableServices = new string[0];
                }

                if (DisableServices == null)
                {
                    DisableServices = new string[0];
                }

                foreach (var service in EnableServices)
                {
                    serviceManager.EnableService(service);
                }

                foreach (var service in DisableServices)
                {
                    serviceManager.DisableService(service);
                }

                if (DebugServiceResolution)
                {
                    serviceManager.EnableDebugInformation();
                }

                try
                {
                    services = serviceManager.CalculateDependencyGraph(loadedProjects.Select(x => x.Project).ToList());
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
                        LogMessage("Enabled service: " + service.FullName);
                    }
                }
            }
            else
            {
                services = serviceManager.LoadServiceSpec(ServiceSpecPath);
                serviceSpecPath = new TemporaryServiceSpec(ServiceSpecPath, true);
            }

            using (serviceSpecPath)
            {
                // Run Protobuild in batch mode in each of the submodules
                // where it is present.
                foreach (var submodule in module.GetSubmodules(Platform))
                {
                    if (_featureManager.IsFeatureEnabledInSubmodule(module, submodule,
                        Feature.OptimizationSkipInvocationOnNoStandardProjects))
                    {
                        if (submodule.GetDefinitionsRecursively(Platform).All(x => !x.IsStandardProject))
                        {
                            // Do not invoke this submodule.
                            LogMessage(
                                "Skipping submodule generation for " + submodule.Name +
                                " (there are no projects to generate)");
                            continue;
                        }
                    }

                    LogMessage(
                        "Invoking submodule generation for " + submodule.Name);
                    var noResolve = _featureManager.IsFeatureEnabledInSubmodule(module, submodule,
                        Feature.PackageManagementNoResolve)
                        ? " -no-resolve"
                        : string.Empty;
                    var noHostPlatform = DisableHostPlatformGeneration &&
                                         _featureManager.IsFeatureEnabledInSubmodule(module, submodule,
                                             Feature.NoHostGenerate)
                        ? " -no-host-generate"
                        : string.Empty;
                    _moduleExecution.RunProtobuild(
                        submodule,
                        _featureManager.GetFeatureArgumentToPassToSubmodule(module, submodule) +
                        "-generate " + Platform +
                        " -spec " + serviceSpecPath +
                        " " + m_PackageRedirector.GetRedirectionArguments() +
                        noResolve + noHostPlatform);
                    LogMessage(
                        "Finished submodule generation for " + submodule.Name);
                }

                var repositoryPaths = new List<string>();

                foreach (var definition in definitions.Where(x => x.ModulePath == module.Path))
                {
                    if (definition.PostBuildHook && RequiresHostPlatform != null)
                    {
                        // We require the host platform projects at this point.
                        RequiresHostPlatform();
                    }

                    string repositoryPath;
                    var definitionCopy = definition;
                    m_ProjectGenerator.Generate(
                        definition,
                        loadedProjects,
                        RootPath,
                        definition.Name,
                        Platform,
                        services,
                        out repositoryPath,
                        () => LogMessage("Generating: " + definitionCopy.Name));

                    // Only add repository paths if they should be generated.
                    if (module.GenerateNuGetRepositories && !string.IsNullOrEmpty(repositoryPath))
                    {
                        repositoryPaths.Add(repositoryPath);
                    }
                }

                var solution = Path.Combine(
                    RootPath,
                    ModuleName + "." + Platform + ".sln");
                LogMessage("Generating: (solution)");
                m_SolutionGenerator.Generate(
                    module,
                    loadedProjects.Select(x => x.Project).ToList(),
                    Platform,
                    solution,
                    services,
                    repositoryPaths);

                // Only save the specification cache if we allow synchronisation
                if (module.DisableSynchronisation == null || !module.DisableSynchronisation.Value)
                {
                    var serviceCache = Path.Combine(RootPath, ModuleName + "." + Platform + ".speccache");
                    LogMessage("Saving service specification");
                    File.Copy(serviceSpecPath.Path, serviceCache, true);
                }

                LogMessage(
                    "Generation complete.");
            }

            return true;
        }
    }
}