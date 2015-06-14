namespace Protobuild
{
    public interface IPackageUrlParser
    {
        PackageRef Parse(string url);
    }
}

