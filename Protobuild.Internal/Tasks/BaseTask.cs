using System;

namespace Protobuild
{
    public abstract class BaseTask
    {
        public Action<string> LogMessage
        {
            get
            {
                return Console.WriteLine;
            }
        }

        public abstract bool Execute();
    }
}

