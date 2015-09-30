namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ModuleInfoGetsUpgradedTest : ProtobuildTest
    {
        [Fact]
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

            Assert.True(File.Exists(GetPath(Path.Combine("Build", "Module.xml"))));
            Assert.Equal(@"<?xml version=""1.0"" encoding=""utf-8""?>
<Module>
  <Name>Protobuild</Name>
  <DefaultAction>resync</DefaultAction>
  <GenerateNuGetRepositories>true</GenerateNuGetRepositories>
  <Packages>
    <Package Uri=""http://protobuild.org/hach-que/xunit"" Folder=""xunit"" GitRef=""master"" />
  </Packages>
</Module>", ReadFile(Path.Combine("Build", "Module.xml")).Trim());
        }
    }
}