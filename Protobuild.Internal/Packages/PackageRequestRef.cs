namespace Protobuild
{
    internal class PackageRequestRef
    {
        public PackageRequestRef(
            string uri,
            string gitRef,
            string platform,
            bool forceUpgrade,
            bool isStaticReference)
        {
            Uri = uri;
            GitRef = gitRef;
            Platform = platform;
            ForceUpgrade = forceUpgrade;
            IsStaticReference = isStaticReference;
        }

        public bool IsStaticReference { get; }

        public bool ForceUpgrade { get; }

        public string Uri { get; set; }

        public string GitRef { get; }

        public string Platform { get; }
    }
}