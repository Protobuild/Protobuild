namespace Protobuild
{
    public interface IPackageGlobalTool
    {
        string GetGlobalToolInstallationPath(PackageRef reference);

        void ScanPackageForToolsAndInstall(string toolFolder);

        string ResolveGlobalToolIfPresent(string toolName);
    }
}

