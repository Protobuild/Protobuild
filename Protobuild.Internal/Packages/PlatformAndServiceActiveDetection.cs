using System;

namespace Protobuild
{
    public class PlatformAndServiceActiveDetection
    {
        // TODO: Find some way of merging this class with the C# XSLT generator.

        public bool ProjectAndServiceIsActive(
            string platformString,
            string includePlatformString,
            string excludePlatformString,
            string serviceString,
            string includeServiceString,
            string excludeServiceString,
            string activePlatform,
            string activeServicesString)
        {
            if (!ProjectIsActive(platformString, includePlatformString, excludePlatformString, activePlatform))
            {
                return false;
            }

            return ServiceIsActive(serviceString, includeServiceString, excludeServiceString, activeServicesString);
        }

        public bool ProjectIsActive(
            string platformString,
            string includePlatformString,
            string excludePlatformString,
            string activePlatform)
        {
            // Choose either <Platforms> or <IncludePlatforms>
            if (string.IsNullOrEmpty(platformString))
            {
                platformString = includePlatformString;
            }

            // If the exclude string is set, then we must check this first.
            if (!string.IsNullOrEmpty(excludePlatformString))
            {
                var excludePlatforms = excludePlatformString.Split(',');
                foreach (var i in excludePlatforms)
                {
                    if (i == activePlatform)
                    {
                        // This platform is excluded.
                        return false;
                    }
                }
            }

            // If the platform string is empty at this point, then we allow
            // all platforms since there's no whitelist of platforms configured.
            if (string.IsNullOrEmpty(platformString))
            {
                return true;
            }

            // Otherwise ensure the platform is in the include list.
            var platforms = platformString.Split(',');
            foreach (var i in platforms)
            {
                if (i == activePlatform)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ServiceIsActive(
            string serviceString,
            string includeServiceString,
            string excludeServiceString,
            string activeServicesString)
        {
            var activeServices = activeServicesString.Split(',');

            // Choose either <Services> or <IncludeServices>
            if (string.IsNullOrEmpty(serviceString))
            {
                serviceString = includeServiceString;
            }

            // If the exclude string is set, then we must check this first.
            if (!string.IsNullOrEmpty(excludeServiceString))
            {
                var excludeServices = excludeServiceString.Split(',');
                foreach (var i in excludeServices)
                {
                    if (System.Linq.Enumerable.Contains(activeServices, i))
                    {
                        // This service is excluded.
                        return false;
                    }
                }
            }

            // If the service string is empty at this point, then we allow
            // all services since there's no whitelist of services configured.
            if (string.IsNullOrEmpty(serviceString))
            {
                return true;
            }

            // Otherwise ensure the service is in the include list.
            var services = serviceString.Split(',');
            foreach (var i in services)
            {
                if (System.Linq.Enumerable.Contains(activeServices, i))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

