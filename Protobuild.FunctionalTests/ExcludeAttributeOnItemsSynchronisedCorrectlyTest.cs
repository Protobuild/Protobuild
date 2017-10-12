namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExcludeAttributeOnItemsSynchronisedCorrectlyTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExcludeAttributeOnItemsSynchronisedCorrectlyTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ExcludeAttributeOnItemsSynchronisedCorrectly");

            // Reset the definition back to it's original version for the test.
            var original = this.GetPath(@"Build\OriginalProjects\Console.definition");
            var target = this.GetPath(@"Build\Projects\Console.definition");
            File.Copy(original, target, true);

            this.OtherMode("sync", "Windows", purge: false);

            var targetContents = this.ReadFile(@"Build\Projects\Console.definition");

            try
            {
                _assert.Contains("Exclude=\"Program.cs\"", targetContents);
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