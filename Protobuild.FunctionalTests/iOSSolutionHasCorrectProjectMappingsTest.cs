using System;
using System.IO;
using Prototest.Library.Version1;

namespace Protobuild.Tests
{
    public class iOSSolutionHasCorrectProjectMappingsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public iOSSolutionHasCorrectProjectMappingsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("iOSSolutionHasCorrectProjectMappings");

            this.Generate("iOS");

            _assert.True(File.Exists(this.GetPath(@"Module.iOS.sln")));

            var solution = this.ReadFile(@"Module.iOS.sln");

            _assert.Contains(@".AppStore|iPhone.Build.0", solution);
        }
    }
}

