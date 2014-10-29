using System;
using System.IO;
using LZMA;

namespace Protobuild.Compress
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("No input or output file specified for compression.");
                Environment.Exit(1);
            }

            if (args.Length == 1)
            {
                Console.WriteLine("No output file specified for compression.");
                Environment.Exit(1);
            }

            var srcFile = args[0];
            var destFile = args[1];

            using (var reader = new FileStream(srcFile, FileMode.Open, FileAccess.Read))
            {
                using (var writer = new FileStream(destFile, FileMode.Create, FileAccess.Write))
                {
                    LzmaHelper.Compress(reader, writer);
                }
            }

            Console.WriteLine(srcFile + " compressed as " + destFile);
        }
    }
}

