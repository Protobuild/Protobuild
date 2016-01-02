namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageTemplateIgnoresTemplateOnlyFolderTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageTemplateIgnoresTemplateOnlyFolderTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GeneratedTemplateIsCorrect()
        {
            this.SetupTest("PackageTemplateIgnoresTemplateOnlyFolder", true);

            // Make sure the Generated directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Generated")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("Generated"));
            }

            Directory.CreateDirectory(GetPath("Generated"));

            var templateFolder = this.SetupSrcTemplate();
            
            this.OtherMode(
                "start", "local-template-git://" + templateFolder, workingSubdirectory: "Generated");

            _assert.False(Directory.Exists(GetPath(Path.Combine("Generated", "_TemplateOnly"))));
            _assert.False(File.Exists(GetPath(Path.Combine("Generated", "_TemplateOnly"))));
        }
    }
}
