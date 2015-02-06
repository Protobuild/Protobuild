namespace Protobuild
{
    public interface IPackageCache
    {
        bool HasBinaryPackage(string url, string gitHash, string platform, out string format);

        bool HasSourcePackage(string url, string gitHash);

        IPackageContent GetBinaryPackage(string url, string gitHash, string platform);

        IPackageContent GetSourcePackage(string url, string gitHash);
    }
}

