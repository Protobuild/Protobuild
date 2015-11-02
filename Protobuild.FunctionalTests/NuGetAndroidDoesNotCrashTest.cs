namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class NuGetAndroidDoesNotCrashTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public NuGetAndroidDoesNotCrashTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }

        public void GenerationIsCorrect()
        {
            this.SetupTest("NuGetAndroidDoesNotCrash");

            this.Generate("Android");

            _assert.True(File.Exists(this.GetPath(@"Module.Android.sln")));
            _assert.True(File.Exists(this.GetPath(@"Console\Console.Android.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Android.csproj");

            _assert.Contains("MonoAndroid10", consoleContents);
            _assert.Contains("MonoAndroid403", consoleContents);
            _assert.Contains("MonoAndroid41", consoleContents);
        }
    }
}