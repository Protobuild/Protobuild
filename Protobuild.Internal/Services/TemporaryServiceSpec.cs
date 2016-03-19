using System;
using System.IO;

namespace Protobuild
{
    internal class TemporaryServiceSpec : IDisposable
    {
        private readonly string _path;
        private readonly bool _noDelete;

        public TemporaryServiceSpec(string path, bool noDelete = false)
        {
            _path = path;
            _noDelete = noDelete;
        }

        public string Path => _path;

        public void Dispose()
        {
            if (!_noDelete)
            {
                File.Delete(_path);
            }
        }

        public override string ToString()
        {
            return _path;
        }
    }
}