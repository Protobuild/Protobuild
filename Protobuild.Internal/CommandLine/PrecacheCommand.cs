using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Protobuild
{
    internal class PrecacheCommand : ICommand
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;
        private readonly IPackageManager _packageManager;
        private readonly IFeatureManager _featureManager;
        private readonly IPackageUrlParser _packageUrlParser;

        public PrecacheCommand(
            IHostPlatformDetector hostPlatformDetector,
            IPackageManager packageManager,
            IFeatureManager featureManager,
            IPackageUrlParser packageUrlParser)
        {
            _hostPlatformDetector = hostPlatformDetector;
            _packageManager = packageManager;
            _featureManager = featureManager;
            _packageUrlParser = packageUrlParser;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -precache option");
            }

            pendingExecution.PackageUrl = args[0];

            if (args.Length > 1)
            {
                pendingExecution.PrecacheSource = args[1] == "true";
            }

            if (args.Length > 2)
            {
                pendingExecution.Platform = args[2];
            }
        }

        public int Execute(Execution execution)
        {
            var platforms = execution.Platform ?? this._hostPlatformDetector.DetectPlatform();

            var package = _packageUrlParser.Parse(execution.PackageUrl);

            foreach (var platform in platforms.Split(','))
            {
                // Create a temporary working directory where we can precache files.
                var tempDir = Path.Combine(Path.GetTempPath(), "precache-" + HashString(execution.PackageUrl + "|" + platform + "|" + (execution.PrecacheSource == null ? "null" : execution.PrecacheSource.Value ? "true" : "false")));
                if (Directory.Exists(tempDir))
                {
                    PathUtils.AggressiveDirectoryDelete(tempDir);
                }
                Directory.CreateDirectory(tempDir);

                try
                {
                    RedirectableConsole.WriteLine("Precaching " + package.Uri + "...");
                    var metadata = _packageManager.Lookup(tempDir, null, package, platform, null, execution.PrecacheSource, true, false);
                    _packageManager.Resolve(tempDir, metadata, package, "PRECACHE", execution.PrecacheSource, true, false);

                    // Also precache dependencies.
                    if (File.Exists(Path.Combine(tempDir, "Build", "Module.xml")))
                    {
                        var moduleInfo = ModuleInfo.Load(Path.Combine(tempDir, "Build", "Module.xml"));
                        _packageManager.ResolveAll(tempDir, moduleInfo, platform, execution.UseTaskParallelisation, true, false, execution.PrecacheSource);
                    }
                }
                finally
                {
                    PathUtils.AggressiveDirectoryDelete(tempDir);
                }
            }

            return 0;
        }

        private string HashString(string str)
        {
            var sha1 = new SHA1Managed();
            var hashed = sha1.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToString(hashed).ToLowerInvariant().Replace("-", "");
        }

        public string GetDescription()
        {
            return @"
Downloads and precaches the specified package, and all of it's dependencies.
";
        }

        public int GetArgCount()
        {
            return 3;
        }

        public string[] GetArgNames()
        {
            return new[] { "package_url", "cache_source?", "platform?" };
        }

        public bool IsInternal()
        {
            return false;
        }

        public bool IsRecognised()
        {
            return true;
        }

        public bool IsIgnored()
        {
            return !_featureManager.IsFeatureEnabled(Feature.PackageManagement);
        }
    }
}

