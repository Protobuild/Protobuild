namespace Protobuild
{
    internal interface IPackageNameLookup
    {
        PackageRef LookupPackageByName(ModuleInfo module, string url);
    }
}