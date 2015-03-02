namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageLocationTwoNestedSubmoduleSamePackageTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageLocationTwoNestedSubmoduleSamePackage");

            var src = this.SetupSrcPackage();

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

            Assert.False(File.Exists(this.GetPath("Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath("SubmoduleA\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath("SubmoduleB\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath("SubmoduleA\\NestedSubmoduleA\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath("SubmoduleB\\NestedSubmoduleB\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath("Package\\.redirect")));
            Assert.False(File.Exists(this.GetPath("SubmoduleA\\Package\\.redirect")));
            Assert.False(File.Exists(this.GetPath("SubmoduleB\\Package\\.redirect")));
            Assert.False(File.Exists(this.GetPath("SubmoduleA\\NestedSubmoduleA\\Package\\.redirect")));
            Assert.True(File.Exists(this.GetPath("SubmoduleB\\NestedSubmoduleB\\Package\\.redirect")));
            Assert.True(File.Exists(this.GetPath("SubmoduleA\\NestedSubmoduleA\\NestedLibraryA\\NestedLibraryA.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath("SubmoduleB\\NestedSubmoduleB\\NestedLibraryB\\NestedLibraryB.Windows.csproj")));

            var nestedLibraryAContents = this.ReadFile("SubmoduleA\\NestedSubmoduleA\\NestedLibraryA\\NestedLibraryA.Windows.csproj");
            var nestedLibraryBContents = this.ReadFile("SubmoduleB\\NestedSubmoduleB\\NestedLibraryB\\NestedLibraryB.Windows.csproj");

            Assert.Contains(
                @"Include=""..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                nestedLibraryAContents);
            Assert.Contains(
                @"Include=""..\..\..\SubmoduleA\NestedSubmoduleA\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                nestedLibraryBContents);
        }
    }
}