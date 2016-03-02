using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protobuild.Tests;
using Prototest.Library.Version1;

namespace Protobuild.FunctionalTests
{
    public class PackageAutomaticDoesNotCopyIncludeReferenceTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageAutomaticDoesNotCopyIncludeReferenceTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void PackageIsCorrect()
        {
            this.SetupTest("PackageAutomaticDoesNotCopyIncludeReference", isPackTest: true);

            this.OtherMode("pack", args: ". Windows.tar.lzma Windows", purge: false);

            var packagedFiles = this.LoadPackage("Windows.tar.lzma");

            _assert.Contains("Build/", packagedFiles.Keys);
            _assert.Contains("Build/Module.xml", packagedFiles.Keys);
            _assert.Contains("Build/Projects/", packagedFiles.Keys);
            _assert.Contains("Build/Projects/ExternalProject.definition", packagedFiles.Keys);
            _assert.Contains("Build/Projects/IncludeProject.definition", packagedFiles.Keys);
            _assert.Contains("Build/Projects/StandardProject.definition", packagedFiles.Keys);

            using (var stream = new MemoryStream(packagedFiles["Build/Projects/StandardProject.definition"]))
            {
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    
                    _assert.Contains("ExternalProject", content);
                    _assert.DoesNotContain("IncludeProject", content);
                }
            }
        }
    }
}
