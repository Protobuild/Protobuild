using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    internal class PackageRedirector : IPackageRedirector
    {
        private IPackageCacheConfiguration m_PackageCacheConfiguration;

        private Dictionary<string, string> m_LocalRedirects = new Dictionary<string, string>();

        public PackageRedirector(IPackageCacheConfiguration packageCacheConfiguration)
        {
            this.m_PackageCacheConfiguration = packageCacheConfiguration;
        }

        public string RedirectPackageUrl(string url)
        {
            foreach (var kv in this.m_LocalRedirects)
            {
                if (url == kv.Key)
                {
                    Console.WriteLine("Redirecting package from " + kv.Key + " to " + kv.Value + " due to command line options");
                    return kv.Value;
                }
            }

            var redirects = this.m_PackageCacheConfiguration.GetRedirectsFile();

            if (!File.Exists(redirects))
            {
                return url;
            }

            var redirectMappings = new Dictionary<string, string>();
            using (var reader = new StreamReader(redirects))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine().Trim();
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }

                    var components = line.Split(new[] { "->" }, StringSplitOptions.None);
                    if (components.Length >= 2)
                    {
                        var original = components[0].Trim();
                        var replace = components[1].Trim();

                        if (url == original)
                        {
                            Console.WriteLine("Redirecting package from " + original + " to " + replace + " due to configuration in " + redirects);
                            return replace;
                        }
                    }
                }
            }

            return url;
        }

        public void RegisterLocalRedirect(string original, string replacement)
        {
            this.m_LocalRedirects[original] = replacement;
        }

        public string GetRedirectionArguments()
        {
            return string.Join(" ", this.m_LocalRedirects.Select(kv => "-redirect \"" + kv.Key + "\" \"" + kv.Value + "\""));
        }
    }
}

