using System.Collections.Generic;

namespace Protobuild.Internal
{
    public class ProtobuildPackageMetadata : IPackageMetadata
    {
        public string PackageType { get; set; }

        public string SourceURI { get; set; }

        public Dictionary<string, string> DownloadMap { get; set; }

        public Dictionary<string, string> ArchiveTypeMap { get; set; }

        public Dictionary<string, string> ResolvedHash { get; set; }
    }
}
