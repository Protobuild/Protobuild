namespace Protobuild
{
    internal interface IPackageTransformer
    {
        byte[] Transform(string workingDirectory, string url, string gitReference, string platform, string format);
    }
}

