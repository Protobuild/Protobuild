namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class SubmodulePackageResolutionSkippedIfNoPackagesOrSubmodulesTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public SubmodulePackageResolutionSkippedIfNoPackagesOrSubmodulesTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("SubmodulePackageResolutionSkippedIfNoPackagesOrSubmodules");

            var output = this.Generate(capture: true);
            
            _assert.Contains("Skipping package resolution in submodule for Submodule (there are no submodule or packages)", output.Item1);
        }
    }
}