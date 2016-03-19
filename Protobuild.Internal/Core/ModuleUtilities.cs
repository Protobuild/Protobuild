using System;
using System.Linq;

namespace Protobuild
{
    internal class ModuleUtilities : IModuleUtilities
    {
        private readonly IFeatureManager _featureManager;

        public ModuleUtilities(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        /// <summary>
        /// Normalizes the platform string from user input, automatically correcting case
        /// and validating against a list of supported platforms.
        /// </summary>
        /// <returns>The platform string.</returns>
        /// <param name="module">The module to normalize the platform string for.</param>
        /// <param name="platform">The normalized platform string.</param>
        public string NormalizePlatform(ModuleInfo module, string platform)
        {
            var supportedPlatforms = ModuleInfo.GetSupportedPlatformsDefault();
            var defaultPlatforms = true;

            if (!string.IsNullOrEmpty(module.SupportedPlatforms))
            {
                supportedPlatforms = module.SupportedPlatforms;
                defaultPlatforms = false;
            }

            var supportedPlatformsArray = supportedPlatforms.Split(new[] { ',' })
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            var allowWeb = _featureManager.IsFeatureEnabled(Feature.PackageManagement);

            // Search the array to find a platform that matches case insensitively
            // to the specified platform.  If we are using the default list, then we allow
            // other platforms to be specified (in case the developer has modified the XSLT to
            // support others but is not using <SupportedPlatforms>).  If the developer has
            // explicitly set the supported platforms, then we return null if the user passes
            // an unknown platform (the caller is expected to exit at this point).
            foreach (var supportedPlatform in supportedPlatformsArray)
            {
                if (string.Compare(supportedPlatform, platform, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    if (supportedPlatform == "Web" && !allowWeb)
                    {
                        // We don't permit the web platform when package management
                        // is disabled, as we can't install JSIL properly.
                        return null;
                    }

                    return supportedPlatform;
                }
            }

            if (defaultPlatforms)
            {
                if (string.Compare("Web", platform, StringComparison.InvariantCultureIgnoreCase) == 0 && !allowWeb)
                {
                    // We don't permit the web platform when package management
                    // is disabled, as we can't install JSIL properly.
                    return null;
                }

                return platform;
            }
            else
            {
                return null;
            }
        }
    }
}

