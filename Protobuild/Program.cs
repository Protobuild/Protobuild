using System;
using System.Reflection;
using System.IO;
using System.IO.Compression;
using LZMA;

namespace Protobuild.Bootstrap
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // This is a small, bootstrapping application that extracts
            // the LZMA compressed Protobuild.Internal library assembly
            // from an embedded resource and then loads it, calling
            // the internal library's Program.Main method instead.
            //
            // This is done so that we can significantly reduce the size
            // of the Protobuild executable shipped in repositories,
            // because most of the application code ends up being LZMA
            // compressed.

            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Protobuild.Internal.dll.lzma");
            var memory = new MemoryStream();
            LzmaHelper.Decompress(stream, memory);

            var bytes = new byte[memory.Position];
            memory.Seek(0, SeekOrigin.Begin);
            memory.Read(bytes, 0, bytes.Length);
            memory.Close();

            var loaded = Assembly.Load(bytes);
            var realProgram = loaded.GetType("Protobuild.MainClass");
            var main = realProgram.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
            main.Invoke(null, new[] { (string[])args });
        }
    }
}

