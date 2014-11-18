namespace Protobuild
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
        }
    }
}