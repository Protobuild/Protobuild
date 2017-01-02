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

            _assert.Equal("monoandroid", platformMapping.GetFrameworkNameForWrite("Android"));
            _assert.Equal("xamarinios", platformMapping.GetFrameworkNameForWrite("iOS"));
            _assert.Equal("xamarintvos", platformMapping.GetFrameworkNameForWrite("tvOS"));
            _assert.Equal("mono40", platformMapping.GetFrameworkNameForWrite("Linux"));
            _assert.Equal("xamarinmac", platformMapping.GetFrameworkNameForWrite("MacOS"));
            _assert.Equal("monoandroid", platformMapping.GetFrameworkNameForWrite("Ouya"));
            _assert.Equal("netstandard1.1", platformMapping.GetFrameworkNameForWrite("PCL"));
            _assert.Equal("net45", platformMapping.GetFrameworkNameForWrite("Windows"));
            _assert.Equal("win8", platformMapping.GetFrameworkNameForWrite("Windows8"));
            _assert.Equal("wp8", platformMapping.GetFrameworkNameForWrite("WindowsPhone"));
            _assert.Equal("wp81", platformMapping.GetFrameworkNameForWrite("WindowsPhone81"));
            _assert.Equal("uap", platformMapping.GetFrameworkNameForWrite("WindowsUniversal"));
            _assert.Equal("net35", platformMapping.GetFrameworkNameForWrite("Unity"));
        }

        public void ReadFrameworksIsCorrectForWindows()
        {
            var kernel = GetKernel();

            var platformMapping = kernel.Get<INuGetPlatformMapping>();

            var windowsPlatforms = platformMapping.GetFrameworkNamesForRead("Windows");

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

