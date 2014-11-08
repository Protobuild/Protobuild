using System;

namespace Protobuild
{
    public class WorkingDirectoryProvider : IWorkingDirectoryProvider
    {
        public string GetPath()
        {
            return Environment.CurrentDirectory;
        }
    }
}

