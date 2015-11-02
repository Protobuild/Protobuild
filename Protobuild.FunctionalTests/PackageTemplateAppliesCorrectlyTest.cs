namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageTemplateAppliesCorrectlyTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageTemplateAppliesCorrectlyTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

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

            _assert.True(Directory.Exists(GetPath(Path.Combine("Generated", "Generated"))));
            _assert.True(Directory.Exists(GetPath(Path.Combine("Generated", "Generated.Content"))));
            _assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated", "GeneratedActivity.cs"))));
            _assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated", "GeneratedGame.cs"))));
            _assert.True(File.Exists(GetPath(Path.Combine("Generated", "Generated", "GeneratedWorld.cs"))));

            var worldFile = ReadFile(Path.Combine("Generated", "Generated", "GeneratedWorld.cs"));

            _assert.Contains("public class GeneratedWorld", worldFile);
            _assert.Contains("Hello Generated!", worldFile);
        }
    }
}
