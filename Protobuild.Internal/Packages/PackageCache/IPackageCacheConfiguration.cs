namespace Protobuild
{
    public interface IPackageCacheConfiguration
    {
        string GetCacheDirectory();

        string GetRedirectsFile();
    }
}

