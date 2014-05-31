using System;

namespace MonoGame.Framework
{
    public class MonoGameExample
    {
        public string Test()
        {
#if SERVICE_DEFAULT
            Console.WriteLine("Default");
#if SERVICE_GL
            Console.WriteLine(" + GL");
#endif
#endif
        }
    }
}