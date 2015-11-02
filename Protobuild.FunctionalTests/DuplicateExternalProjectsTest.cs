namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class DuplicateExternalProjectsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public DuplicateExternalProjectsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("DuplicateExternalProjects");

            this.Generate("Windows");

            _assert.True(File.Exists(this.GetPath(@"DuplicateExternalProjects.Windows.sln")));

            var solutionContents = this.ReadFile(@"DuplicateExternalProjects.Windows.sln");

            _assert.Contains("ThirdParty1.csproj", solutionContents);
            _assert.DoesNotContain("ThirdParty2.csproj", solutionContents);
        }
    }
}