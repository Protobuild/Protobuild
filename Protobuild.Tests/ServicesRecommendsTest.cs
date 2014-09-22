namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class ServicesRecommendsTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesRecommends");

            this.Generate();

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            var libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            Assert.Contains("LIBRARY_SERVICE_B;", libraryContents);

            /*
             * ServiceB is only recommended, so if we explicitly disable it, it should
             * not be present.
             */

            this.Generate(args: "--disable Library/ServiceB");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            Assert.DoesNotContain("LIBRARY_SERVICE_B;", libraryContents);

            /*
             * ServiceA conflicts with ServiceB, so ServiceB automatically gets disabled.
             */

            this.Generate(args: "--enable Library/ServiceA");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            Assert.DoesNotContain("LIBRARY_SERVICE_B;", libraryContents);

            /*
             * ServiceC has no effect on ServiceB, so ServiceB should be enabled.
             */

            this.Generate(args: "--enable Library/ServiceC");

            Assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            Assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            Assert.Contains("LIBRARY_SERVICE_B;", libraryContents);
        }
    }
}