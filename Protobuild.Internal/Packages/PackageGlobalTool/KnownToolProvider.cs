using System;
using System.Collections.Generic;

namespace Protobuild
{
    internal class KnownToolProvider : IKnownToolProvider
    {
        private readonly IPackageGlobalTool _packageGlobalTool;

        private readonly IPackageManager _packageManager;

        private readonly IHostPlatformDetector _hostPlatformDetector;

        private readonly Dictionary<string, string> _knownTools = new Dictionary<string, string>
        {
            {"jsilc", "http://protobuild.org/hach-que/JSIL"},
            {"swig", "http://protobuild.org/hach-que/SWIG"},
            {"protobuild.manager", "http://protobuild.org/hach-que/Protobuild.Manager"},
        };

        public KnownToolProvider(
            IPackageGlobalTool packageGlobalTool,
            IPackageManager packageManager,
            IHostPlatformDetector hostPlatformDetector)
        {
            _packageGlobalTool = packageGlobalTool;
            _packageManager = packageManager;
            _hostPlatformDetector = hostPlatformDetector;
        }

        public string GetToolExecutablePath(string toolName)
        {
            var executableFile = _packageGlobalTool.ResolveGlobalToolIfPresent(toolName);
            if (executableFile == null && _knownTools.ContainsKey(toolName.ToLowerInvariant()))
            {
                var package = new PackageRef
                {
                    Uri = _knownTools[toolName.ToLowerInvariant()],
                    GitRef = "master",
                    Folder = null
                };

                Console.WriteLine("Installing {0}...", _knownTools[toolName.ToLowerInvariant()]);
                _packageManager.Resolve(null, package, _hostPlatformDetector.DetectPlatform(), null, false, true);
            }
            else
            {
                return executableFile;
            }

            return _packageGlobalTool.ResolveGlobalToolIfPresent(toolName);
        }
    }
}
