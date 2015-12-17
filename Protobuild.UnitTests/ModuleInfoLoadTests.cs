using System;
using Prototest.Library.Version1;

namespace Protobuild.UnitTests
{
    public class ModuleInfoLoadTests
    {
        private readonly IAssert _assert;

        public ModuleInfoLoadTests(IAssert assert)
        {
            _assert = assert;
        }

        public void TestFeatureSetIsNullWhenMissing()
        {
            var module = LoadModuleXml("FeatureSetIsNull.xml");

            _assert.Null(module.FeatureSet);
        }

        public void TestFeatureSetIsEmpty()
        {
            var module = LoadModuleXml("FeatureSetIsEmpty.xml");

            _assert.NotNull(module.FeatureSet);
            _assert.Empty(module.FeatureSet);
        }

        public void TestFeatureSetIsPackageManagement()
        {
            var module = LoadModuleXml("FeatureSetIsPackageManagement.xml");

            _assert.NotNull(module.FeatureSet);
            _assert.Equal(1, module.FeatureSet.Count);
            _assert.Contains(Feature.PackageManagement, module.FeatureSet);
        }

        public void TestFeatureSetIsPackageManagementAndHostPlatformGeneration()
        {
            var module = LoadModuleXml("FeatureSetIsPackageManagementAndHostPlatformGeneration.xml");

            _assert.NotNull(module.FeatureSet);
            _assert.Equal(2, module.FeatureSet.Count);
            _assert.Contains(Feature.PackageManagement, module.FeatureSet);
            _assert.Contains(Feature.HostPlatformGeneration, module.FeatureSet);
        }

        private ModuleInfo LoadModuleXml(string manifestStreamName)
        {
            var stream = typeof(ModuleInfoLoadTests).Assembly.GetManifestResourceStream(manifestStreamName);
            return ModuleInfo.Load(stream, null);
        }
    }
}

