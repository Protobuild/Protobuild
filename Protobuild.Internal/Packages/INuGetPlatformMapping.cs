namespace Protobuild
{
    public interface INuGetPlatformMapping
    {
        string GetFrameworkNameForWrite(string workingDirectory, string platform);
        string[] GetFrameworkNamesForRead(string workingDirectory, string platform);
    }
}