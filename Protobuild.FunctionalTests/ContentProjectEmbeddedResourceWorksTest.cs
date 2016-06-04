namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ContentProjectEmbeddedResourceWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ContentProjectEmbeddedResourceWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ContentProjectEmbeddedResourceWorks");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"Console\\Console.Windows.csproj")));

            var projectContents = this.ReadFile(@"Console\\Console.Windows.csproj");

            _assert.Contains("EmbeddedResource", projectContents);
        }
    }
}