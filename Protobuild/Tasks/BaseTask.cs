using System;
using Microsoft.Build.Utilities;

namespace Protobuild
{
    public abstract class BaseTask : Task
    {
        public Action<string> LogMessage
        {
            get
            {
                if (this.Log != null)
                    return x => this.Log.LogMessage(x);
                return Console.WriteLine;
            }
        }
        
    }
}

