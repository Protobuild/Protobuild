namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExcludeAttributeMissingOnItemsThatDontUseItTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExcludeAttributeMissingOnItemsThatDontUseItTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ExcludeAttributeMissingOnItemsThatDontUseIt");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.DoesNotContain("Exclude=", projectContents);
        }
    }
}