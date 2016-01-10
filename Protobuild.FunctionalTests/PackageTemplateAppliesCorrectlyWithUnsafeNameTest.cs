namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageTemplateAppliesCorrectlyWithUnsafeNameTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageTemplateAppliesCorrectlyWithUnsafeNameTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

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
            try
            {
                this.OtherMode(
                    "start", "local-template-git://" + templateFolder + " Generated.WithDot",
                    workingSubdirectory: "Generated");

                _assert.True(Directory.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot"))));
                _assert.True(Directory.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot.Content"))));
                _assert.True(
                    File.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot", "GeneratedWithDotActivity.cs"))));
                _assert.True(
                    File.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot", "GeneratedWithDotGame.cs"))));
                _assert.True(
                    File.Exists(GetPath(Path.Combine("Generated", "Generated.WithDot", "GeneratedWithDotWorld.cs"))));

                var worldFile = ReadFile(Path.Combine("Generated", "Generated.WithDot", "GeneratedWithDotWorld.cs"));

                _assert.Contains("public class GeneratedWithDotWorld", worldFile);
                _assert.Contains("Hello Generated.WithDot!", worldFile);
            }
            finally
            {
                PathUtils.AggressiveDirectoryDelete(templateFolder);
            }
        }
    }
}
