namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class GlobalToolVSIXAndGACInstallWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public GlobalToolVSIXAndGACInstallWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("GlobalToolVSIXAndGACInstallWorks");

            this.OtherMode("install", "local-nuget-v3://./Protobuild.IDE.VisualStudio.Windows.nupkg");
        }
    }
}