namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class SimulateHostPlatformWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public SimulateHostPlatformWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("SimulateHostPlatformWorks");

            var platform = Path.DirectorySeparatorChar == '/' ? "Linux" : "Windows";
            var hostPlatform = Path.DirectorySeparatorChar == '/' ? "Windows" : "Linux";
            var platformPath = this.GetPath(Path.Combine("Console", "Console." + platform + ".csproj"));

            this.Generate(platform: platform, hostPlatform: hostPlatform);

            _assert.True(File.Exists(platformPath));

            var contents = ReadFile(platformPath);
            _assert.Contains("HostPlatform:" + hostPlatform, contents);
        }
    }
}