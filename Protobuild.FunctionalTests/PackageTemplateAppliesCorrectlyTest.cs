namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageTemplateAppliesCorrectlyTest : ProtobuildTest
    {
        [Fact]
        public void GeneratedTemplateIsCorrect()
        {
            this.SetupTest("PackageTemplateAppliesCorrectly", true);

            // Make sure the Generated directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Generated")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("Generated"));
            }

            Directory.CreateDirectory(GetPath("Generated"));

            var templateFolder = this.SetupSrcTemplate();
            
            this.OtherMode(
                "start", "local-template-git://" + templateFolder, workingSubdirectory: "Generated");

            Assert.True(Directory.Exists(GetPath(Path.Combine("Generated", "Generated"))));
            Assert.True(Directory.Exists(GetPath(Path.Combine("Generated", "Generated.Content"))));
            Assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated", "GeneratedActivity.cs"))));
            Assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated", "GeneratedGame.cs"))));
            Assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated", "GeneratedWorld.cs"))));

            var worldFile = ReadFile(Path.Combine("Generated", "Generated", "GeneratedWorld.cs"));

            Assert.Contains("public class GeneratedWorld", worldFile);
            Assert.Contains("Hello Generated!", worldFile);
        }
    }
}
