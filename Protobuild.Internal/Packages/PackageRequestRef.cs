namespace Protobuild
{
    internal class PackageRequestRef
    {
        public PackageRequestRef(
            string uri,
            string gitRef,
            string platform,
            bool preferCacheLookup)
        {
            Uri = uri;
            GitRef = gitRef;
            Platform = platform;
            PreferCacheLookup = preferCacheLookup;
        }

        public string Uri { get; set; }

        public string GitRef { get; }

        public string Platform { get; }

        public bool PreferCacheLookup { get; }
    }
}