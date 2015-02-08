namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class PackageLocationParentAndTwoSubmoduleSamePackageTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("PackageLocationParentAndTwoSubmoduleSamePackage");

            var src = this.SetupSrcPackage();

            // Make sure the Package directory is removed so we have a clean test every time.
            if (Directory.Exists(this.GetPath("Package")))
            {
                Directory.Delete(this.GetPath("Package"), true);
            }
            if (Directory.Exists(this.GetPath("SubmoduleA\\Package")))
            {
                Directory.Delete(this.GetPath("SubmoduleA\\Package"), true);
            }
            if (Directory.Exists(this.GetPath("SubmoduleB\\Package")))
            {
                Directory.Delete(this.GetPath("SubmoduleB\\Package"), true);
            }

            this.Generate(args: "--redirect http://protobuild.org/hach-que/TestEmptyPackage local-git://" + src);

            Assert.True(File.Exists(this.GetPath("Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath("SubmoduleA\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.False(File.Exists(this.GetPath("SubmoduleB\\Package\\PackageLibrary\\PackageLibrary.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath("SubmoduleA\\Package\\.redirect")));
            Assert.True(File.Exists(this.GetPath("SubmoduleB\\Package\\.redirect")));
            Assert.True(File.Exists(this.GetPath("SubmoduleA\\LibraryA\\LibraryA.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath("SubmoduleB\\LibraryB\\LibraryB.Windows.csproj")));

            var consoleContents = this.ReadFile("Console\\Console.Windows.csproj");
            var libraryAContents = this.ReadFile("SubmoduleA\\LibraryA\\LibraryA.Windows.csproj");
            var libraryBContents = this.ReadFile("SubmoduleB\\LibraryB\\LibraryB.Windows.csproj");

            Assert.Contains(
                @"Include=""..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                consoleContents);
            Assert.Contains(
                @"Include=""..\..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                libraryAContents);
            Assert.Contains(
                @"Include=""..\..\Package\PackageLibrary\PackageLibrary.Windows.csproj""",
                libraryBContents);
        }
    }
}