using System;

namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageNuGetHTTPResolvesNewTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageNuGetHTTPResolvesNewTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageNuGetHTTPResolvesNew");

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