namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class SyncDoesNotIncludeContentWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public SyncDoesNotIncludeContentWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void SyncIsCorrect()
        {
            this.SetupTest("SyncDoesNotIncludeContentWorks");

            // Reset the definition back to it's original version for the test.
            var original = this.GetPath(@"Build\OriginalProjects\Console.definition");
            var target = this.GetPath(@"Build\Projects\Console.definition");
            File.Copy(original, target, true);

            this.OtherMode("sync", "Windows", purge: false);

            var targetContents = this.ReadFile(@"Build\Projects\Console.definition");

            try
            {
                _assert.Contains("Program.cs", targetContents);
                _assert.Contains("Added.cs", targetContents);
                _assert.DoesNotContain("Content", targetContents);
            }
            finally
            {
                // Reset the file back after the test passes so that Git doesn't
                // report this file as changed.
                File.Copy(original, target, true);
            }
        }
    }
}