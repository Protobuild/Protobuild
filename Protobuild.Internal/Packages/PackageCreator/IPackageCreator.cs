using System.IO;

namespace Protobuild
{
    internal interface IPackageCreator
    {
        void Create(Stream target, FileFilter filter, string basePath, string packageFormat);
    }
}

