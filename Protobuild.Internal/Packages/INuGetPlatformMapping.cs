namespace Protobuild
{
    public interface INuGetPlatformMapping
    {
        string GetFrameworkNameForWrite(string platform);
        string[] GetFrameworkNamesForRead(string platform);
    }
}