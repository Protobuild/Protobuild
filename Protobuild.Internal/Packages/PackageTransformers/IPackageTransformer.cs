namespace Protobuild
{
    internal interface IPackageTransformer
    {
        byte[] Transform(string url, string gitReference, string platform, string format);
    }
}

