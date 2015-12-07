﻿namespace Protobuild
{
    public static class LightweightKernelModule
    {
        public static void BindCore(this LightweightKernel kernel)
        {
            kernel.Bind<IActionDispatch, ActionDispatch>();
            kernel.Bind<IHostPlatformDetector, HostPlatformDetector>();
            kernel.Bind<ILogger, Logger>();
            kernel.Bind<IWorkingDirectoryProvider, WorkingDirectoryProvider>();
        }

        public static void BindBuildResources(this LightweightKernel kernel)
        {
            kernel.Bind<IResourceProvider, ResourceProvider>();
            kernel.Bind<IGenerationFunctionsProvider, GenerationFunctionsProvider>();
        }

        public static void BindGeneration(this LightweightKernel kernel)
        {
            kernel.Bind<IExcludedServiceAwareProjectDetector, ExcludedServiceAwareProjectDetector>();
            kernel.Bind<IExternalProjectReferenceResolver, ExternalProjectReferenceResolver>();
            kernel.Bind<IContentProjectGenerator, ContentProjectGenerator>();
            kernel.Bind<INuGetConfigMover, NuGetConfigMover>();
            kernel.Bind<INuGetReferenceDetector, NuGetReferenceDetector>();
            kernel.Bind<INuGetRepositoriesConfigGenerator, NuGetRepositoriesConfigGenerator>();
            kernel.Bind<IProjectGenerator, ProjectGenerator>();
            kernel.Bind<IProjectInputGenerator, ProjectInputGenerator>();
            kernel.Bind<IProjectLoader, ProjectLoader>();
            kernel.Bind<IServiceInputGenerator, ServiceInputGenerator>();
            kernel.Bind<IServiceReferenceTranslator, ServiceReferenceTranslator>();
            kernel.Bind<ISolutionGenerator, SolutionGenerator>();
            kernel.Bind<ISolutionInputGenerator, SolutionInputGenerator>();
            kernel.Bind<IPlatformResourcesGenerator, PlatformResourcesGenerator>();
            kernel.Bind<IIncludeProjectAppliesToUpdater, IncludeProjectAppliesToUpdater>();
        }

        public static void BindJSIL(this LightweightKernel kernel)
        {
            kernel.Bind<IJSILProvider, JSILProvider>();
        }

        public static void BindTargets(this LightweightKernel kernel)
        {
            kernel.Bind<ILanguageStringProvider, LanguageStringProvider>();
        }

        public static void BindFileFilter(this LightweightKernel kernel)
        {
            kernel.Bind<IFileFilterParser, FileFilterParser>();
        }

        public static void BindPackages(this LightweightKernel kernel)
        {
            kernel.Bind<IAutomaticModulePackager, AutomaticModulePackager>();
            kernel.Bind<IDeduplicator, Deduplicator>();
            kernel.Bind<IPackageCache, PackageCache>();
            kernel.Bind<IPackageRetrieval, PackageRetrieval>();
            kernel.Bind<IPackageManager, PackageManager>();
            kernel.Bind<IPackageLookup, PackageLookup>();
            kernel.Bind<IPackageCacheConfiguration, PackageCacheConfiguration>();
            kernel.BindAndKeepInstance<IPackageRedirector, PackageRedirector>();
            kernel.Bind<IPackageLocator, PackageLocator>();
            kernel.Bind<IProjectOutputPathCalculator, ProjectOutputPathCalculator>();
            kernel.Bind<IPackageGlobalTool, PackageGlobalTool>();
            kernel.Bind<IProgressiveWebOperation, ProgressiveWebOperation>();
            kernel.Bind<IPackageCreator, PackageCreator>();
            kernel.Bind<IGetRecursiveUtilitiesInPath, GetRecursiveUtilitiesInPath>();
            kernel.Bind<IPackageUrlParser, PackageUrlParser>();
            kernel.Bind<IKnownToolProvider, KnownToolProvider>();
            kernel.Bind<IPackageNameLookup, PackageNameLookup>();
        }

        public static void BindAutomatedBuild(this LightweightKernel kernel)
        {
            kernel.Bind<IAutomatedBuildController, AutomatedBuildController>();
            kernel.Bind<IAutomatedBuildRuntimeV1, AutomatedBuildRuntimeV1>();
        }
    }
}