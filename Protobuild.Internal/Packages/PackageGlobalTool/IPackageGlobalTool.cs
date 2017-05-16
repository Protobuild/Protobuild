namespace Protobuild
{
    public interface IPackageGlobalTool
    {
        string GetGlobalToolInstallationPath(string referenceURI);

        void ScanPackageForToolsAndInstall(string toolFolder, IKnownToolProvider knownToolProvider);

        string ResolveGlobalToolIfPresent(string toolName);
    }
}

