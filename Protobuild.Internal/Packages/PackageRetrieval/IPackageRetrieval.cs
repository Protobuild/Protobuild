using System;

namespace Protobuild
{
    public interface IPackageRetrieval
    {
        bool DownloadBinaryPackage(string uri, string gitHash, string platform, out string format, string targetPath);

        byte[] DownloadBinaryPackage(string uri, string gitHash, string platform, out string format);

        void DownloadSourcePackage(string gitUrl, string targetPath);
    }
}

