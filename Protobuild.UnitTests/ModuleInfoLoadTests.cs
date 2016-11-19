using System;
using System.IO;
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

        public void TestPackagesAreReadCorrectly()
        {
            var module = LoadModuleXml("Package.xml");

            _assert.NotNull(module.Packages);
            _assert.Equal(2, module.Packages.Count);

            _assert.Equal("https://protobuild.org/hach-que/Protobuild", module.Packages[0].Uri);
            _assert.Equal("master", module.Packages[0].GitRef);
            _assert.Equal("Protobuild", module.Packages[0].Folder);

            _assert.Equal("https-nuget-v3://api.nuget.org/v3/index.json|Protobuild", module.Packages[1].Uri);
            _assert.Equal("16.1119.13506", module.Packages[1].GitRef);
            _assert.Equal("Protobuild", module.Packages[1].Folder);
        }

        public void TestPackagesAreWrittenCorrectly()
        {
            var module = LoadModuleXml("Package.xml");

            _assert.NotNull(module.Packages);
            _assert.Equal(2, module.Packages.Count);

            _assert.Equal("https://protobuild.org/hach-que/Protobuild", module.Packages[0].Uri);
            _assert.Equal("master", module.Packages[0].GitRef);
            _assert.Equal("Protobuild", module.Packages[0].Folder);
            
            _assert.Equal("https-nuget-v3://api.nuget.org/v3/index.json|Protobuild", module.Packages[1].Uri);
            _assert.Equal("16.1119.13506", module.Packages[1].GitRef);
            _assert.Equal("Protobuild", module.Packages[1].Folder);

            using (var memory = new MemoryStream())
            {
                module.Save(memory);
                memory.Seek(0, SeekOrigin.Begin);

                var stream = typeof(ModuleInfoLoadTests).Assembly.GetManifestResourceStream("Package.xml");

                using (var reader1 = new StreamReader(memory))
                {
                    using (var reader2 = new StreamReader(stream))
                    {
                        var text1 = reader1.ReadToEnd();
                        var text2 = reader2.ReadToEnd();

                        _assert.Equal(text1, text2);
                    }
                }
            }
        }

        private ModuleInfo LoadModuleXml(string manifestStreamName)
        {
            var stream = typeof(ModuleInfoLoadTests).Assembly.GetManifestResourceStream(manifestStreamName);
            return ModuleInfo.Load(stream, null);
        }
    }
}

