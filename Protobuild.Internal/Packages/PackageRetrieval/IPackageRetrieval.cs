using System;

namespace Protobuild
{
    public interface IPackageRetrieval
    {
        bool DownloadBinaryPackage(string uri, string gitHash, string platform, out string format, string targetPath);

        void DownloadSourcePackage(string gitUrl, string targetPath);
    }
}

