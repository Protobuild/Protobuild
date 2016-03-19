using System;

namespace tar_cs
{
    internal class TarException : Exception
    {
        public TarException(string message) : base(message)
        {
        }
    }
}