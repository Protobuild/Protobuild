namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ExternalCPlusPlusProjectReferencesAreCorrectInSolutionTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ExternalCPlusPlusProjectReferencesAreCorrectInSolutionTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ExternalCPlusPlusProjectReferencesAreCorrectInSolution");

            this.Generate("Windows", hostPlatform: "Windows");

            _assert.True(File.Exists(this.GetPath(@"Module.Windows.sln")));

            var solutionContents = this.ReadFile(@"Module.Windows.sln");

            _assert.Contains(
                "Project(\"{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}\") = \"cppProjectName\", \"cppProjectName.vcxproj\", \"{1DBEF0FF-4EF7-4B4A-AF94-32A0CCB25547}\"",
                solutionContents);
        }
    }
}