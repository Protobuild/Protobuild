namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageRedirectionToLocalPathTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageRedirectionToLocalPath");

            var src = this.SetupSrcPackage();

            // Make sure the Package directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Package")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
            }

            this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

            Assert.True(File.Exists(this.GetPath("Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
        }
    }
}