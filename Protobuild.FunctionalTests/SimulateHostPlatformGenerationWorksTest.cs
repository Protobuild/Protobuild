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

            var hostPlatform = Path.DirectorySeparatorChar == '/' ? "Windows" : "Linux";
            var hostPlatformPath = this.GetPath("Module." + hostPlatform + ".sln");

            if (File.Exists(hostPlatformPath))
            {
                File.Delete(hostPlatformPath);
            }

            this.Generate(hostPlatform: hostPlatform);

            _assert.True(File.Exists(hostPlatformPath));
        }
    }
}