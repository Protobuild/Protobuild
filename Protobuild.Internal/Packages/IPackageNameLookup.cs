namespace Protobuild
{
    public interface IPackageNameLookup
    {
        PackageRef LookupPackageByName(ModuleInfo module, string url);
    }
}