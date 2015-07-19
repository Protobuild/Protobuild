namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageResolutionDoesNotOccurInSubmodulesTwiceTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageResolutionDoesNotOccurInSubmodulesTwice");

            var src = this.SetupSrcPackage();

            // Make sure the Package directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Package")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
            }

            var stdout = this.Generate(
                args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src,
                capture: true).Item1;

            var idxSubmoduleGeneration = stdout.IndexOf("Invoking submodule generation for Submodule", System.StringComparison.InvariantCulture);
            Assert.NotEqual(-1, idxSubmoduleGeneration);

            var substrStdout = stdout.Substring(idxSubmoduleGeneration);
            var idxPackageResolution = substrStdout.IndexOf("Starting resolution of packages...", System.StringComparison.InvariantCulture);

            // We should not see any package resolution we invoke submodule generation.
            Assert.Equal(-1, idxPackageResolution);
        }
    }
}