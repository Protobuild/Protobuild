using System;

namespace Protobuild.Bootstrap
{
    /// <summary>
    /// The debug version of the bootstrapping program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The entry point for the bootstrapping program.
        /// </summary>
        /// <param name="args">The arguments passed in on the command line.</param>
        public static void Main(string[] args)
        {
            // This is an alternative version of the standard Protobuild.exe
            // assembly, which references Protobuild.Internal directly.  This
            // is here so that Protobuild can be debugged from an IDE, and have
            // the correct debugging symbols loaded and present.
            //
            // The bootstrapping process would normally otherwise remove
            // debugging symbols, because the Protobuild.Internal assembly is
            // compressed and dynamically loaded as a byte stream rather than
            // using a simple reference (the latter is what this version does
            // but is obviously not suitable for shipping a single executable).
            //
            // This debugging version isn't deployed anywhere, and should only
            // be used for the explicit purpose of debugging Protobuild.

            ExecEnvironment.RunProtobuildInProcess = true;
            ExecEnvironment.DoNotWrapExecutionInTry = true;
            Protobuild.MainClass.Main(Environment.CurrentDirectory, args);
        }
    }
}

