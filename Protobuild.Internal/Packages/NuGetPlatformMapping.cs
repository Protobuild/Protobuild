using System.Linq;
using System.Xml;

namespace Protobuild
{
    internal class NuGetPlatformMapping : INuGetPlatformMapping
    {
        private readonly IResourceProvider _resourceProvider;

        public NuGetPlatformMapping(IResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
        }

        public string GetFrameworkNameForWrite(string platform)
        {
            var nugetPlatformMappings = _resourceProvider.LoadXML(ResourceType.NuGetPlatformMappings, Language.CSharp, platform);

            return nugetPlatformMappings.SelectSingleNode("/NuGetPlatformMappings/Platform[@Name='" + platform + "']/WriteFrameworkName")?.InnerText;
        }

        public string[] GetFrameworkNamesForRead(string platform)
        {
            var nugetPlatformMappings = _resourceProvider.LoadXML(ResourceType.NuGetPlatformMappings, Language.CSharp, platform);

            var frameworks = nugetPlatformMappings.SelectNodes("/NuGetPlatformMappings/Platform[@Name='" + platform + "']/ReadFrameworkNames/Framework");
            if (frameworks == null)
            {
                return new[]
                {
                    "=net45",
                    "=Net45",
                    "=net40-client",
                    "=Net40-client",
                    "=net403",
                    "=Net403",
                    "=net40",
                    "=Net40",
                    "=net35-client",
                    "=Net35-client",
                    "=net20",
                    "=Net20",
                    "=net11",
                    "=Net11",
                    "=20",
                    "=11",
                    "=",
                    "?net45",
                    "?Net45",
                    "?net4",
                    "?Net4",
                };
            }

            return frameworks.OfType<XmlElement>().Select(x => x.InnerText).ToArray();
        }
    }
}
