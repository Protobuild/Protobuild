namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageProtobuildHTTPResolvesSecondTimeForSourceOnlyTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageProtobuildHTTPResolvesSecondTimeForSourceOnlyTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageProtobuildHTTPResolvesSecondTimeForSourceOnly");

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
            this.OtherMode("resolve", "Windows");
        }
    }
}