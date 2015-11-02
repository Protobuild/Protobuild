namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class ServicesRecommendsTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public ServicesRecommendsTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("ServicesRecommends");

            this.Generate();

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            var libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.Contains("LIBRARY_SERVICE_B;", libraryContents);

            /*
             * ServiceB is only recommended, so if we explicitly disable it, it should
             * not be present.
             */

            this.Generate(args: "--disable Library/ServiceB");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.DoesNotContain("LIBRARY_SERVICE_B;", libraryContents);

            /*
             * ServiceA conflicts with ServiceB, so ServiceB automatically gets disabled.
             */

            this.Generate(args: "--enable Library/ServiceA");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.DoesNotContain("LIBRARY_SERVICE_B;", libraryContents);

            /*
             * ServiceC has no effect on ServiceB, so ServiceB should be enabled.
             */

            this.Generate(args: "--enable Library/ServiceC");

            _assert.True(File.Exists(this.GetPath(@"Console\Console.Windows.csproj")));
            _assert.True(File.Exists(this.GetPath(@"Submodule\Library\Library.Windows.csproj")));

            libraryContents = this.ReadFile(@"Submodule\Library\Library.Windows.csproj");

            _assert.Contains("LIBRARY_SERVICE_B;", libraryContents);
        }
    }
}