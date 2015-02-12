namespace Protobuild.Tests
{
    using System.IO;
    using Xunit;

    public class NuGetAndroidDoesNotCrashTest : ProtobuildTest
    {
        [Fact]
        public void GenerationIsCorrect()
        {
            this.SetupTest("NuGetAndroidDoesNotCrash");

            this.Generate("Android");

            Assert.True(File.Exists(this.GetPath(@"Module.Android.sln")));
            Assert.True(File.Exists(this.GetPath(@"Console\Console.Android.csproj")));

            var consoleContents = this.ReadFile(@"Console\Console.Android.csproj");

            Assert.Contains("MonoAndroid10", consoleContents);
            Assert.Contains("MonoAndroid403", consoleContents);
            Assert.Contains("MonoAndroid41", consoleContents);
        }
    }
}