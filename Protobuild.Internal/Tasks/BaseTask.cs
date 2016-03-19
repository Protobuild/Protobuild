using System;

namespace Protobuild
{
    internal abstract class BaseTask
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

