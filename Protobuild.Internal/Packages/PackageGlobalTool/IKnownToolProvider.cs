namespace Protobuild
{
    public interface IKnownToolProvider
    {
        string GetToolExecutablePath(string toolName);
    }
}
