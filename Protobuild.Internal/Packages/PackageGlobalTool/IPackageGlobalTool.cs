namespace Protobuild
{
    public interface IPackageGlobalTool
    {
        string GetGlobalToolInstallationPath(string referenceURI);

        void ScanPackageForToolsAndInstall(string toolFolder);

        string ResolveGlobalToolIfPresent(string toolName);
    }
}

