using System;

namespace Protobuild
{
    internal class WorkingDirectoryProvider : IWorkingDirectoryProvider
    {
        public string GetPath()
        {
            return Environment.CurrentDirectory;
        }
    }
}

