using System;
using Prototest.Library.Version1;
using System.Collections.Generic;

namespace Protobuild.UnitTests
{
    public class FeatureManagerTests
    {
        private readonly IAssert _assert;

        public FeatureManagerTests(IAssert assert)
        {
            _assert = assert;
        }

        private LightweightKernel GetKernel(bool noFeaturePropagation = false)
        {
            var kernel = new LightweightKernel();
            kernel.Bind<IFeatureManager, FeatureManager>();
            if (noFeaturePropagation)
            {
                kernel.Bind<IModuleExecution, MockModuleExecutionNoFeatureSupport>();
            }
            else
            {
                kernel.Bind<IModuleExecution, MockModuleExecution>();
            }
            return kernel;
        }

        private class MockModuleExecution : IModuleExecution
        {
            public Tuple<int, string, string> RunProtobuild (ModuleInfo module, string args, bool capture = false)
            {
                return new Tuple<int, string, string>(
                    0,
                    @"propagate-features",
                    string.Empty);
            }
        }

        private class MockModuleExecutionNoFeatureSupport : IModuleExecution
        {
            public Tuple<int, string, string> RunProtobuild (ModuleInfo module, string args, bool capture = false)
            {
                return new Tuple<int, string, string>(
                    0,
                    @"",
                    string.Empty);
            }
        }

        public void NoFeaturesEnabledWhenModuleHasNoFeaturesAndCommandLineFeaturesNotSpecified()
        {
            var kernel = GetKernel();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine(null);
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>()
                });

            _assert.False(featureManager.IsFeatureEnabled(Feature.PackageManagement));
            _assert.False(featureManager.IsFeatureEnabled(Feature.HostPlatformGeneration));
        }

        public void AllFeaturesEnabledWhenModuleHasNoFeaturesButCommandLineIsFull()
        {
            var kernel = GetKernel();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine("full");
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>()
                });

            _assert.True(featureManager.IsFeatureEnabled(Feature.PackageManagement));
            _assert.True(featureManager.IsFeatureEnabled(Feature.HostPlatformGeneration));
        }

        public void AllFeaturesEnabledWhenModuleHasNoFeaturesButCommandLineHasAllFeatures()
        {
            var kernel = GetKernel();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine("PackageManagement,HostPlatformGeneration");
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>()
                });

            _assert.True(featureManager.IsFeatureEnabled(Feature.PackageManagement));
            _assert.True(featureManager.IsFeatureEnabled(Feature.HostPlatformGeneration));
        }

        public void NoFeaturesEnabledWhenModuleHasNoFeaturesButCommandLineIsEmptyString()
        {
            var kernel = GetKernel();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine("");
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>()
                });

            _assert.False(featureManager.IsFeatureEnabled(Feature.PackageManagement));
            _assert.False(featureManager.IsFeatureEnabled(Feature.HostPlatformGeneration));
        }

        public void PackageManagementFeatureIsEnabledWhenModuleHasNoFeaturesButCommandLineSpecifiesIt()
        {
            var kernel = GetKernel();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine("PackageManagement");
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>()
                });

            _assert.True(featureManager.IsFeatureEnabled(Feature.PackageManagement));
            _assert.False(featureManager.IsFeatureEnabled(Feature.HostPlatformGeneration));
        }

        public void CommandLineToPassToSubmoduleIsEmptyWhenFeaturesAreNotSpecifiedOnCommandLine()
        {
            var kernel = GetKernel();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine(null);
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>()
                });

            var module = new ModuleInfo();
            var submodule = new ModuleInfo();

            _assert.Equal("--features \"\" ", featureManager.GetFeatureArgumentToPassToSubmodule(module, submodule));
        }

        public void CommandLineToPassToSubmoduleIsFullWhenFeaturesAreFullOnCommandLine()
        {
            var kernel = GetKernel();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine("full");
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>()
                });

            var module = new ModuleInfo();
            var submodule = new ModuleInfo();

            _assert.Equal("--features full ", featureManager.GetFeatureArgumentToPassToSubmodule(module, submodule));
        }

        public void CommandLineToPassToSubmoduleHasPackageManagementWhenFeaturesAreNotSpecifiedOnCommandLine()
        {
            var kernel = GetKernel();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine(null);
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>
                    {
                        Feature.PackageManagement
                    }
                });

            var module = new ModuleInfo();
            var submodule = new ModuleInfo();

            _assert.Equal("--features \"PackageManagement\" ", featureManager.GetFeatureArgumentToPassToSubmodule(module, submodule));
        }

        public void CommandLineToPassToSubmoduleIsEmptyWhenFeaturePropagationIsNotSupported()
        {
            var kernel = GetKernel(true);

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine(null);
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>
                    {
                        Feature.PackageManagement
                    }
                });

            var module = new ModuleInfo();
            var submodule = new ModuleInfo();

            _assert.Equal(string.Empty, featureManager.GetFeatureArgumentToPassToSubmodule(module, submodule));
        }

        public void QueryFeaturesWithNoFeaturesEnabledReturnsCorrectValue()
        {
            var kernel = GetKernel(true);

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine(null);
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>()
                });

            var features = featureManager.GetEnabledInternalFeatureIDs();

            _assert.Equal(6, features.Length);
            _assert.Contains("query-features", features);
            _assert.Contains("skip-invocation-on-no-standard-projects", features);
            _assert.Contains("skip-synchronisation-on-no-standard-projects", features);
            _assert.Contains("skip-resolution-on-no-packages-or-submodules", features);
            _assert.Contains("inline-invocation-if-identical-hashed-executables", features);
            _assert.Contains("propagate-features", features);
        }

        public void QueryFeaturesWithOnlyPackageManagementFeatureEnabledReturnsCorrectValue()
        {
            var kernel = GetKernel(true);

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine(null);
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = new List<Feature>
                    {
                        Feature.PackageManagement
                    }
                });

            var features = featureManager.GetEnabledInternalFeatureIDs();

            _assert.Equal(8, features.Length);
            _assert.Contains("query-features", features);
            _assert.Contains("no-resolve", features);
            _assert.Contains("list-packages", features);
            _assert.Contains("skip-invocation-on-no-standard-projects", features);
            _assert.Contains("skip-synchronisation-on-no-standard-projects", features);
            _assert.Contains("skip-resolution-on-no-packages-or-submodules", features);
            _assert.Contains("inline-invocation-if-identical-hashed-executables", features);
            _assert.Contains("propagate-features", features);
        }

        public void QueryFeaturesWithAllFeaturesEnabledReturnsCorrectValue()
        {
            var kernel = GetKernel(true);

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesFromCommandLine(null);
            featureManager.LoadFeaturesFromSpecificModule(new ModuleInfo
                {
                    FeatureSet = null
                });

            var features = featureManager.GetEnabledInternalFeatureIDs();

            _assert.Equal(9, features.Length);
            _assert.Contains("query-features", features);
            _assert.Contains("no-resolve", features);
            _assert.Contains("list-packages", features);
            _assert.Contains("skip-invocation-on-no-standard-projects", features);
            _assert.Contains("skip-synchronisation-on-no-standard-projects", features);
            _assert.Contains("skip-resolution-on-no-packages-or-submodules", features);
            _assert.Contains("inline-invocation-if-identical-hashed-executables", features);
            _assert.Contains("no-host-generate", features);
            _assert.Contains("propagate-features", features);
        }
    }
}

