using System;

namespace Protobuild
{
    internal class Logger : ILogger
    {
        public void Log(string message)
        {
            RedirectableConsole.WriteLine(message);
        }
    }
}

