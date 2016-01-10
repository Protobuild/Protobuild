namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageLocationTwoNestedSubmoduleSamePackageTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageLocationTwoNestedSubmoduleSamePackageTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageLocationTwoNestedSubmoduleSamePackage");

            var src = this.SetupSrcPackage();
            try
            {
                // Make sure the Package directory is removed so we have a clean test every time.
                if (Directory.Exists(this.GetPath("SubmoduleA\\NestedSubmoduleA\\Package")))
                {
                    PathUtils.AggressiveDirectoryDelete(this.GetPath("SubmoduleA\\NestedSubmoduleA\\Package"));
                }
                if (Directory.Exists(this.GetPath("SubmoduleB\\NestedSubmoduleB\\Package")))
                {
                    PathUtils.AggressiveDirectoryDelete(this.GetPath("SubmoduleB\\NestedSubmoduleB\\Package"));
                }

                this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

                _assert.False(File.Exists(this.GetPath("Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
                _assert.False(
                    File.Exists(this.GetPath("SubmoduleA\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
                _assert.False(
                    File.Exists(this.GetPath("SubmoduleB\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
                _assert.True(
                    File.Exists(
                        this.GetPath(
                            "SubmoduleA\\NestedSubmoduleA\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
                _assert.False(
                    File.Exists(
                        this.GetPath(
                            "SubmoduleB\\NestedSubmoduleB\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
                _assert.False(File.Exists(this.GetPath("Package\\.redirect")));
                _assert.False(File.Exists(this.GetPath("SubmoduleA\\Package\\.redirect")));
                _assert.False(File.Exists(this.GetPath("SubmoduleB\\Package\\.redirect")));
                _assert.False(File.Exists(this.GetPath("SubmoduleA\\NestedSubmoduleA\\Package\\.redirect")));
                _assert.True(File.Exists(this.GetPath("SubmoduleB\\NestedSubmoduleB\\Package\\.redirect")));
                _assert.True(
                    File.Exists(
                        this.GetPath("SubmoduleA\\NestedSubmoduleA\\NestedLibraryA\\NestedLibraryA.Windows.csproj")));
                _assert.True(
                    File.Exists(
                        this.GetPath("SubmoduleB\\NestedSubmoduleB\\NestedLibraryB\\NestedLibraryB.Windows.csproj")));

                var nestedLibraryAContents =
                    this.ReadFile("SubmoduleA\\NestedSubmoduleA\\NestedLibraryA\\NestedLibraryA.Windows.csproj");
                var nestedLibraryBContents =
                    this.ReadFile("SubmoduleB\\NestedSubmoduleB\\NestedLibraryB\\NestedLibraryB.Windows.csproj");

                _assert.Contains(
                    @"Include=""..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                    nestedLibraryAContents);
                _assert.Contains(
                    @"Include=""..\..\..\SubmoduleA\NestedSubmoduleA\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                    nestedLibraryBContents);
            }
            finally
            {
                PathUtils.AggressiveDirectoryDelete(src);
            }
        }
    }
}