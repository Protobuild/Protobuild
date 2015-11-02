namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ForcePCLGenerationWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ForcePCLGenerationWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ForcePCLGenerationWorks");

            this.Generate("Windows");
        }
    }
}