namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageTemplateAppliesCorrectlyWithUnsafeNameTest : ProtobuildTest
    {
        [Fact]
        public void GeneratedTemplateIsCorrect()
        {
            this.SetupTest("PackageTemplateAppliesCorrectlyWithUnsafeName", true);

            // Make sure the Generated directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Generated")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("Generated"));
            }

            Directory.CreateDirectory(GetPath("Generated"));

            var templateFolder = this.SetupSrcTemplate();

            this.OtherMode(
                "start", "local-template-git://" + templateFolder + " Generated.WithDot", workingSubdirectory: "Generated");

            Assert.True(Directory.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot"))));
            Assert.True(Directory.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot.Content"))));
            Assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot", "GeneratedWithDotActivity.cs"))));
            Assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot", "GeneratedWithDotGame.cs"))));
            Assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot", "GeneratedWithDotWorld.cs"))));

            var worldFile = ReadFile(Path.Combine("Generated", "Generated.WithDot", "GeneratedWithDotWorld.cs"));

            Assert.Contains("public class GeneratedWithDotWorld", worldFile);
            Assert.Contains("Hello Generated.WithDot!", worldFile);
        }
    }
}
