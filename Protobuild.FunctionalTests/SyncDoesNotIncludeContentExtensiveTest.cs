namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class SyncDoesNotIncludeContentExtensiveTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public SyncDoesNotIncludeContentExtensiveTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void SyncIsCorrect()
        {
            this.SetupTest("SyncDoesNotIncludeContentExtensive");

            // Reset the definition back to it's original version for the test.
            var original = this.GetPath(@"Build\OriginalProjects\HMRQ.definition");
            var target = this.GetPath(@"Build\Projects\HMRQ.definition");
            File.Copy(original, target, true);

            this.OtherMode("sync", "Windows", purge: false);

            var targetContents = this.ReadFile(@"Build\Projects\HMRQ.definition");

            try
            {
                _assert.Contains("Program.cs", targetContents);
                _assert.Contains("Furnace.cs", targetContents);
                _assert.DoesNotContain(@"..\HMRQ.Content\assets\.source", targetContents);
                _assert.DoesNotContain(@"..\HMRQ.Content\assets\audio\Jump.wav", targetContents);
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