namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class BasicSynchronisationWorksTest : ProtobuildTest
    {
        public BasicSynchronisationWorksTest(IAssert assert) : base(assert)
        {
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("BasicSynchronisationWorks");

            this.OtherMode("resync", "Linux");
            this.OtherMode("resync", "Linux", purge: false);
            this.OtherMode("resync", "Linux", purge: false);
        }
    }
}