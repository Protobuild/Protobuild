#pragma warning disable 1591

namespace Protobuild
{
    public enum Feature
    {
        [FeatureInternal("query-features")]
        QueryFeatures,

        [FeatureInternal("no-resolve")]
        [FeatureDependsOn(Feature.PackageManagement)]
        PackageManagementNoResolve,

        [FeatureInternal("list-packages")]
        [FeatureDependsOn(Feature.PackageManagement)]
        PackageManagementListPackages,

        [FeatureInternal("skip-invocation-on-no-standard-projects")]
        OptimizationSkipInvocationOnNoStandardProjects,

        [FeatureInternal("skip-synchronisation-on-no-standard-projects")]
        OptimizationSkipSynchronisationOnNoStandardProjects,

        [FeatureInternal("skip-resolution-on-no-packages-or-submodules")]
        OptimizationSkipResolutionOnNoPackagesOrSubmodules,

        [FeatureInternal("inline-invocation-if-identical-hashed-executables")]
        InlineInvocationIfIdenticalHashedExecutables,

        [FeatureInternal("no-host-generate")]
        [FeatureDependsOn(Feature.HostPlatformGeneration)]
        NoHostGenerate,

        [FeatureInternal("propagate-features")]
        PropagateFeatures,

        [FeatureInternal("task-parallelisation")]
        TaskParallelisation,

        [FeatureDescription(
            "The package management features of Protobuild.",
            new[]
            {
                "The --add command is not available",
                "The --pack command is not available",
                "The --push command is not available",
                "The --repush command is not available",
                "The --resolve command is ignored",
                "The --list command is not available",
                "The --install command is not available",
                "The --upgrade command is not available",
                "The --upgrade-all command is not available",
                "The --redirect command is not available",
                "The --swap-to-binary command is not available",
                "The --swap-to-source command is not available",
                "The --format command is not available",
                "The --package-root command is ignored",
                "The --no-resolve command is ignored",
                "The --ignore-on-existing-package command is ignored",
                "The <Packages> node in Module.xml is entirely ignored",
                "No package resolution occurs during any step of Protobuild",
                "Git submodules will not be cloned",
                "Protobuild packages will not be downloaded or referenced",
                "NuGet packages will not be downloaded or referenced",
                "No external tools can be installed",
                "Referencing C++ projects from C# projects with SWIG bindings will not work",
                "The Web platform will not work as JSIL can not be installed",
                "None of the package related commands inside automated.build will work",
                "Package redirections (the .redirect file) will be ignored",
                "Git submodule and package references will not be deduplicated in the module tree",
            })]
        PackageManagement,

        [FeatureDescription(
            "Generates host platforms in solutions when required.",
            new[]
            {
                "Post-build hooks will cause build errors if the host platform is not built manually",
                "IDE editor integrations will not be available",
            })]
        HostPlatformGeneration,

        [FeatureDescription(
            "Turns off safe package resolution by default.",
            new[]
            {
                "Package directories will not be removed if needed upon package resolution",
                "Builds may fail when files are missing due to package resolution skipping directories",
            })]
        SafeResolutionDisabled,
    }
}

