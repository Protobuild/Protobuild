namespace Protobuild
{
    internal interface IKnownToolProvider
    {
        string GetToolExecutablePath(string toolName);
    }
}
