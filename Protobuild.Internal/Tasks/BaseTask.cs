using System;

namespace Protobuild
{
    internal abstract class BaseTask
    {
        public Action<string> LogMessage
        {
            get
            {
                return RedirectableConsole.WriteLine;
            }
        }

        public abstract bool Execute();
    }
}

