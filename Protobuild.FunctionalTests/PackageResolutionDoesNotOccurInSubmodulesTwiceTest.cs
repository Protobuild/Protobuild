namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageResolutionDoesNotOccurInSubmodulesTwiceTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageResolutionDoesNotOccurInSubmodulesTwiceTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageResolutionDoesNotOccurInSubmodulesTwice");

            var src = this.SetupSrcPackage();
            try
            {
                // Make sure the Package directory is removed so we have a clean test every time.
                if (Directory.Exists(this.GetPath("Package")))
                {
                    PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
                }

                var platform = "Windows";
                if (Path.DirectorySeparatorChar == '/')
                {
                    platform = "Linux";

                    if (Directory.Exists("/Library"))
                    {
                        platform = "MacOS";
                    }
                }

                var stdout = this.Generate(
                    platform: platform,
                    args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src,
                    capture: true).Item1;

                var idxSubmoduleGeneration = stdout.IndexOf("Invoking submodule generation for Submodule",
                    System.StringComparison.InvariantCulture);
                _assert.NotEqual(-1, idxSubmoduleGeneration);

                var substrStdout = stdout.Substring(idxSubmoduleGeneration);
                var idxPackageResolution =
                    substrStdout.IndexOf("Starting resolution of packages for " + platform + "...",
                        System.StringComparison.InvariantCulture);

                // We should not see any package resolution we invoke submodule generation.
                _assert.Equal(-1, idxPackageResolution);
            }
            finally
            {
                PathUtils.AggressiveDirectoryDelete(src);
            }
        }
    }
}