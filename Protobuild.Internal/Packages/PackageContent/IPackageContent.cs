using System;

namespace Protobuild
{
    public interface IPackageContent
    {
        void ExtractTo(string path);
    }
}

