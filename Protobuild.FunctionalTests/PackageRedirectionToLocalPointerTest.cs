namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageRedirectionToLocalPointerTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageRedirectionToLocalPointer");

            var src = this.SetupSrcPackage();

            // Make sure the Package directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Package")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
            }

            this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-pointer://" + src);

            // Pointers should create a .redirect file which Protobuild uses to then link
            // across the folder hierarchy.
            Assert.True(File.Exists(this.GetPath("Package\\.redirect")));
        }
    }
}
