namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class PackageLocationTwoSubmoduleSamePackageTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public PackageLocationTwoSubmoduleSamePackageTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageLocationTwoSubmoduleSamePackage");

            var src = this.SetupSrcPackage();

            // Make sure the Package directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("SubmoduleA\\Package")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("SubmoduleA\\Package"));
            }
            if (Directory.Exists(this.GetPath("SubmoduleB\\Package")))
            {
                PathUtils.AggressiveDirectoryDelete(this.GetPath("SubmoduleB\\Package"));
            }

            this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

            _assert.False(File.Exists(this.GetPath("Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath("SubmoduleA\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath("SubmoduleB\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            _assert.False(File.Exists(this.GetPath("SubmoduleA\\Package\\.redirect")));
            _assert.True(File.Exists(this.GetPath("SubmoduleB\\Package\\.redirect")));
            _assert.True(File.Exists(this.GetPath("SubmoduleA\\LibraryA\\LibraryA.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath("SubmoduleB\\LibraryB\\LibraryB.Windows.csproj")));

            var libraryAContents = this.ReadFile("SubmoduleA\\LibraryA\\LibraryA.Windows.csproj");
            var libraryBContents = this.ReadFile("SubmoduleB\\LibraryB\\LibraryB.Windows.csproj");

            _assert.Contains(
                @"Include=""..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                libraryAContents);
            _assert.Contains(
                @"Include=""..\..\SubmoduleA\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                libraryBContents);
        }
    }
}