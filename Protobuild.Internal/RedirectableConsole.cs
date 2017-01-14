using System;
using System.Collections.Generic;

namespace Protobuild
{
    public class RedirectableBuffer
    {
        public RedirectableBuffer()
        {
            Stdout = string.Empty;
            Stderr = string.Empty;
        }

        public string Stdout { get; set; }
        public string Stderr { get; set; }
    }

    public static class RedirectableConsole
    {
        [ThreadStatic]
        public static RedirectableBuffer TargetBuffer;

        public static void Write(object content)
        {
            if (TargetBuffer != null)
            {
                TargetBuffer.Stdout += content ?? "";
            }
            else
            {
                Console.Write(content);
            }
        }

        public static void WriteLine()
        {
            if (TargetBuffer != null)
            {
                TargetBuffer.Stdout += "\n";
            }
            else
            {
                Console.WriteLine();
            }
        }

        public static void WriteLine(object content)
        {
            if (TargetBuffer != null)
            {
                TargetBuffer.Stdout += (content ?? "") + "\n";
            }
            else
            {
                Console.WriteLine(content);
            }
        }

        public static void WriteLine(string content)
        {
            if (TargetBuffer != null)
            {
                TargetBuffer.Stdout += (content ?? "") + "\n";
            }
            else
            {
                Console.WriteLine(content);
            }
        }

        public static void WriteLine(string format, params object[] args)
        {
            if (TargetBuffer != null)
            {
                TargetBuffer.Stdout += string.Format(format, args) + "\n";
            }
            else
            {
                Console.WriteLine(format, args);
            }
        }

        public static void ErrorWrite(object content)
        {
            if (TargetBuffer != null)
            {
                TargetBuffer.Stderr += content ?? "";
            }
            else
            {
                Console.Error.Write(content);
            }
        }

        public static void ErrorWriteLine(object content)
        {
            if (TargetBuffer != null)
            {
                TargetBuffer.Stderr += (content ?? "") + "\n";
            }
            else
            {
                Console.Error.WriteLine(content);
            }
        }
    }
}
