namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ModuleInfoGetsUpgradedTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ModuleInfoGetsUpgradedTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ModuleInfoGetsUpgraded");

            if (File.Exists(GetPath(Path.Combine("Build", "Module.xml"))))
            {
                File.Delete(GetPath(Path.Combine("Build", "Module.xml")));
            }

            File.Copy(
                GetPath(Path.Combine("Build", "ModuleUnconverted.xml")),
                GetPath(Path.Combine("Build", "Module.xml")));

            this.Generate("Windows");

            _assert.True(File.Exists(GetPath(Path.Combine("Build", "Module.xml"))));
            _assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Module>
  <Name>Protobuild</Name>
  <DefaultAction>resync</DefaultAction>
  <GenerateNuGetRepositories>true</GenerateNuGetRepositories>
  <Packages>
    <Package Uri=""http://protobuild.org/hach-que/xunit"" Folder=""xunit"" GitRef=""master"" />
  </Packages>
</Module>".Replace("\r\n", "\n"), ReadFile(Path.Combine("Build", "Module.xml")).Trim().Replace("\r\n", "\n"));
        }
    }
}
