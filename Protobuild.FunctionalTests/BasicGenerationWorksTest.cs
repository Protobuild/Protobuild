namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class BasicGenerationWorksTest : ProtobuildTest
    {
        public BasicGenerationWorksTest(IAssert assert) : base(assert)
        {
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("BasicGenerationWorks");

            this.Generate("Windows");
        }
    }
}