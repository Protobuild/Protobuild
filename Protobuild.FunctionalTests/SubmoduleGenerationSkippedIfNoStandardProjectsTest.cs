namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class SubmoduleGenerationSkippedIfNoStandardProjectsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public SubmoduleGenerationSkippedIfNoStandardProjectsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("SubmoduleGenerationSkippedIfNoStandardProjects");

            var output = this.Generate(capture: true);

            _assert.False(File.Exists(this.GetPath("Submodule\\Submodule.Windows.sln")));
            _assert.Contains("Skipping submodule generation for Submodule (there are no projects to generate)", output.Item1);
        }
    }
}