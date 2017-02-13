namespace Protobuild
{
    /// <remarks>
    /// This MUST remain public so that it's accessible to the XSLT generation functions!
    /// </remarks>
    public interface IKnownToolProvider
    {
        string GetToolExecutablePath(string toolName);
    }
}
