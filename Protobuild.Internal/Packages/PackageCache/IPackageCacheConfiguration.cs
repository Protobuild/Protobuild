namespace Protobuild
{
    internal interface IPackageCacheConfiguration
    {
        string GetCacheDirectory();

        string GetRedirectsFile();
    }
}

