namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageResolutionDoesNotOccurWhenPackageManagementDisabledTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageResolutionDoesNotOccurWhenPackageManagementDisabledTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageResolutionDoesNotOccurWhenPackageManagementDisabled");

            // Make sure the Package directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Package")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
            }

            var output = this.Generate(capture: true);

            _assert.DoesNotContain("Starting resolution of packages for", output.Item1);
        }
    }
}