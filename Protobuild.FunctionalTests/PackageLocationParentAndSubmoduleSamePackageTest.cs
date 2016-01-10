namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageLocationParentAndSubmoduleSamePackageTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageLocationParentAndSubmoduleSamePackageTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageLocationParentAndSubmoduleSamePackage");

            var src = this.SetupSrcPackage();
            try
            {
                // Make sure the Package directory is removed so we have a clean test every time.
                if (Directory.Exists(this.GetPath("Package")))
                {
                    PathUtils.AggressiveDirectoryDelete(this.GetPath("Package"));
                }

                this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

                _assert.True(File.Exists(this.GetPath("Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
                _assert.True(File.Exists(this.GetPath("Submodule\\Package\\.redirect")));
                _assert.True(File.Exists(this.GetPath("Submodule\\Library\\Library.Windows.csproj")));
                _assert.True(File.Exists(this.GetPath("Console\\Console.Windows.csproj")));

                var consoleContents = this.ReadFile("Console\\Console.Windows.csproj");
                var libraryContents = this.ReadFile("Submodule\\Library\\Library.Windows.csproj");

                _assert.Contains(
                    @"Include=""..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                    consoleContents);
                _assert.Contains(
                    @"Include=""..\..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                    libraryContents);
            }
            finally
            {
                PathUtils.AggressiveDirectoryDelete(src);
            }
        }
    }
}