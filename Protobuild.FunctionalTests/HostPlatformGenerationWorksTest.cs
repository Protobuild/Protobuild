namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class HostPlatformGenerationWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public HostPlatformGenerationWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("HostPlatformGenerationWorks");

            this.Generate("Android", hostPlatform: "Windows");

            var solutionPath = this.GetPath("Module.Android.sln");

            _assert.True(File.Exists(solutionPath));

            var solutionContents = this.ReadFile(solutionPath);

            _assert.Contains("Console.Android.csproj", solutionContents);
            _assert.Contains("Console.Windows.csproj", solutionContents);
            _assert.DoesNotContain("Console.Linux.csproj", solutionContents);
            _assert.DoesNotContain("Console.MacOS.csproj", solutionContents);
        }
    }
}