namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageResolutionDoesNotOccurInFolderWithExistingDataTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageResolutionDoesNotOccurInFolderWithExistingDataTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageResolutionDoesNotOccurInFolderWithExistingData");

            var src = this.SetupSrcPackage();
            try
            {
                this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

                _assert.True(File.Exists(this.GetPath("Package\\empty.txt")));
                _assert.False(File.Exists(this.GetPath("Package\\.pkg")));
                _assert.False(File.Exists(this.GetPath("Package\\.git")));
                _assert.False(Directory.Exists(this.GetPath("Package\\.git")));
            }
            finally
            {
                PathUtils.AggressiveDirectoryDelete(src);
            }
        }
    }
}