namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExcludeAttributeOnItemsGeneratedCorrectlyTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExcludeAttributeOnItemsGeneratedCorrectlyTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ExcludeAttributeOnItemsGeneratedCorrectly");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\Console.Windows.csproj");

            _assert.Contains("<Compile Include=\"*\" Exclude=\"Program.cs\" />", projectContents);
        }
    }
}