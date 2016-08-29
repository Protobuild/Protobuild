namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class SafeResolveDefaultsWithFeatureSetTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public SafeResolveDefaultsWithFeatureSetTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("SafeResolveDefaultsWithFeatureSet");
            
            if (Directory.Exists(this.GetPath("Package")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
            }

            var src = this.SetupSrcPackage();
            try
            {
                Directory.CreateDirectory(GetPath("Package"));
                File.Copy(GetPath("PackageTemp\\Test.txt"), GetPath("Package\\Test.txt"));

                this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

                _assert.False(File.Exists(GetPath("Package\\Test.txt")));
                _assert.True(Directory.Exists(GetPath("Package\\.git")));
            }
            finally
            {
                PathUtils.AggressiveDirectoryDelete(src);

                if (Directory.Exists(this.GetPath("Package")))
                {
                    PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
                }
            }
        }
    }
}