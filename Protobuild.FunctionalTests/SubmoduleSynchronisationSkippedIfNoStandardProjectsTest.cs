namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class SubmoduleSynchronisationSkippedIfNoStandardProjectsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public SubmoduleSynchronisationSkippedIfNoStandardProjectsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void SyncIsCorrect()
        {
            this.SetupTest("SubmoduleSynchronisationSkippedIfNoStandardProjects");
            
            var output = this.OtherMode("sync", capture: true);
            
            _assert.Contains("Skipping submodule synchronisation for Submodule (there are no projects to synchronise)", output.Item1);
        }
    }
}