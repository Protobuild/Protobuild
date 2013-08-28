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
                    return x =>
                        {
                            try
                            {
                                this.Log.LogMessage(x);
                            }
                            catch (InvalidOperationException)
                            {
                                // Under Windows, Log is not null but an exception
                                // is thrown when you try to use LogMessage.
                                Console.WriteLine(x);
                            }
                        };
                return Console.WriteLine;
            }
        }
        
    }
}

