using System.IO;

namespace Protobuild
{
    public interface IPackageCreator
    {
        void Create(Stream target, FileFilter filter, string basePath, string packageFormat);
    }
}

