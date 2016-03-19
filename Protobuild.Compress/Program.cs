using System;
using System.IO;
using LZMA;

namespace Protobuild.Compress
{
    /// <summary>
    /// A program which compresses files so they can be embedded inside the single
    /// Protobuild executable that is produced.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the compression utility.
        /// </summary>
        /// <param name="args">The arguments passed in on the command line.</param>
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

