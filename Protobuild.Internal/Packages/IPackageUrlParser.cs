namespace Protobuild
{
    internal interface IPackageUrlParser
    {
        PackageRef Parse(string url);
    }
}

