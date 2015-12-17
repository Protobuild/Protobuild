﻿namespace Protobuild.Tests
{
    using System.IO;
    using Prototest.Library.Version1;

    public class HostPlatformGenerationWorksTest : ProtobuildTest
    {
        private readonly IAssert _assert;

        public HostPlatformGenerationWorksTest(IAssert assert) : base(assert)
        {
            _assert = assert;
        }
        
        public void GenerationIsCorrect()
        {
            this.SetupTest("HostPlatformGenerationWorks");

            var @out = this.Generate("Android", hostPlatform: "Windows", capture: true);

            var solutionPath = this.GetPath("Module.Android.sln");

            _assert.True(File.Exists(solutionPath));

            var solutionContents = this.ReadFile(solutionPath);

            _assert.Contains("Console.Android.csproj", solutionContents);
            _assert.Contains("Console.Windows.csproj", solutionContents);
            _assert.DoesNotContain("Console.Linux.csproj", solutionContents);
            _assert.DoesNotContain("Console.MacOS.csproj", solutionContents);

            _assert.Contains("Starting generation of projects for Android", @out.Item1);
            _assert.Contains("Starting generation of projects for Windows", @out.Item1);
            _assert.Contains("One or more projects required the presence of host platform", @out.Item1);
        }
    }
}