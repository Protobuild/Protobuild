namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageNuGetHTTPSResolvesNewTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageNuGetHTTPSResolvesNewTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageNuGetHTTPSResolvesNew");

            if (Directory.Exists(this.GetPath("Package")))
            {
                try
                {
                    Directory.Delete(this.GetPath("Package"), true);
                }
                catch
                {
                }
            }

            this.OtherMode("resolve", "Windows");
        }
    }
}