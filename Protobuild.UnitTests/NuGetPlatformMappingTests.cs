using System;
using Prototest.Library.Version1;
using System.Collections.Generic;

namespace Protobuild.UnitTests
{
    public class NuGetPlatformMappingTests
    {
        private readonly IAssert _assert;

        public NuGetPlatformMappingTests(IAssert assert)
        {
            _assert = assert;
        }

        private LightweightKernel GetKernel(bool noFeaturePropagation = false)
        {
            var kernel = new LightweightKernel();
            kernel.BindAll();
            return kernel;
        }
        
        public void WriteFrameworksAreCorrect()
        {
            var kernel = GetKernel();

            var platformMapping = kernel.Get<INuGetPlatformMapping>();
            var workingDirectory = Environment.CurrentDirectory;

            _assert.Equal("monoandroid", platformMapping.GetFrameworkNameForWrite(workingDirectory, "Android"));
            _assert.Equal("xamarinios", platformMapping.GetFrameworkNameForWrite(workingDirectory, "iOS"));
            _assert.Equal("xamarintvos", platformMapping.GetFrameworkNameForWrite(workingDirectory, "tvOS"));
            _assert.Equal("mono40", platformMapping.GetFrameworkNameForWrite(workingDirectory, "Linux"));
            _assert.Equal("xamarinmac", platformMapping.GetFrameworkNameForWrite(workingDirectory, "MacOS"));
            _assert.Equal("monoandroid", platformMapping.GetFrameworkNameForWrite(workingDirectory, "Ouya"));
            _assert.Equal("netstandard1.1", platformMapping.GetFrameworkNameForWrite(workingDirectory, "PCL"));
            _assert.Equal("net45", platformMapping.GetFrameworkNameForWrite(workingDirectory, "Windows"));
            _assert.Equal("win8", platformMapping.GetFrameworkNameForWrite(workingDirectory, "Windows8"));
            _assert.Equal("wp8", platformMapping.GetFrameworkNameForWrite(workingDirectory, "WindowsPhone"));
            _assert.Equal("wp81", platformMapping.GetFrameworkNameForWrite(workingDirectory, "WindowsPhone81"));
            _assert.Equal("uap", platformMapping.GetFrameworkNameForWrite(workingDirectory, "WindowsUniversal"));
            _assert.Equal("net35", platformMapping.GetFrameworkNameForWrite(workingDirectory, "Unity"));
        }

        public void ReadFrameworksIsCorrectForWindows()
        {
            var kernel = GetKernel();

            var platformMapping = kernel.Get<INuGetPlatformMapping>();
            var workingDirectory = Environment.CurrentDirectory;

            var windowsPlatforms = platformMapping.GetFrameworkNamesForRead(workingDirectory, "Windows");

            _assert.Contains("=net45", windowsPlatforms);
            _assert.Contains("=Net45", windowsPlatforms);
            _assert.Contains("=net40-client", windowsPlatforms);
            _assert.Contains("=Net40-client", windowsPlatforms);
            _assert.Contains("=net403", windowsPlatforms);
            _assert.Contains("=Net403", windowsPlatforms);
            _assert.Contains("=net40", windowsPlatforms);
            _assert.Contains("=Net40", windowsPlatforms);
            _assert.Contains("=net35-client", windowsPlatforms);
            _assert.Contains("=Net35-client", windowsPlatforms);
            _assert.Contains("=net20", windowsPlatforms);
            _assert.Contains("=Net20", windowsPlatforms);
            _assert.Contains("=net11", windowsPlatforms);
            _assert.Contains("=Net11", windowsPlatforms);
        }
    }
}

