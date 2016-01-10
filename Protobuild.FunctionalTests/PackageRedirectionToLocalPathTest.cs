namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageRedirectionToLocalPathTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageRedirectionToLocalPathTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageRedirectionToLocalPath");

            var src = this.SetupSrcPackage();
            try
            {
                // Make sure the Package directory is removed so we have a clean test every time.
                if (Directory.Exists(this.GetPath("Package")))
                {
                    PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
                }

                this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

                _assert.True(File.Exists(this.GetPath("Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            }
            finally
            {
                PathUtils.AggressiveDirectoryDelete(src);
            }
        }
    }
}