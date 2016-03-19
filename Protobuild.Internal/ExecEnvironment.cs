using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace Protobuild
{
    /// <summary>
    /// The execution environment of the Protobuild process.  These settings control how
    /// Protobuild executes, how it exits and how invocation of new Protobuild processes
    /// is handled.
    /// </summary>
    public static class ExecEnvironment
    {
        /// <summary>
        /// Always run Protobuild in-process, and never fork.  This is a debugging option
        /// that is used by Protobuild.Debug to ensure that the debugging session continues
        /// into submodules.
        /// </summary>
        public static bool RunProtobuildInProcess = false;
        
        /// <summary>
        /// Do not wrap the execution of Protobuild in a try-catch.  This is a debugging option
        /// that is used by Protobuild.Debug to ensure that exceptions are caught by Visual Studio
        /// or MonoDevelop instead of consumed by the executable.
        /// </summary>
        public static bool DoNotWrapExecutionInTry = false;

        private static int _selfInvokeCounter = 0;

        /// <summary>
        /// Self-invokes Protobuild.  This is used to run Protobuild from it's main entry point
        /// again.
        /// </summary>
        /// <param name="args">The arguments to pass to the self-invoked Protobuild.</param>
        /// <returns>The exit code.</returns>
        public static int InvokeSelf(string[] args)
        {
            _selfInvokeCounter++;
            try
            {
                MainClass.Main(args);
                return 0;
            }
            catch (SelfInvokeExitException ex)
            {
                return ex.ExitCode;
            }
            finally
            {
                _selfInvokeCounter--;
            }
        }

        /// <summary>
        /// Exits the current Protobuild "process".  This should always be used instead of
        /// <see cref="Environment.Exit"/>, because it takes into account scenarios where Protobuild
        /// is being run in-process instead of forked.
        /// </summary>
        /// <param name="exitCode">The exit code to return.</param>
        public static void Exit(int exitCode)
        {
            if (_selfInvokeCounter == 0)
            {
                Environment.Exit(exitCode);
            }
            else
            {
                throw new SelfInvokeExitException(exitCode);
            }
        }

        /// <summary>
        /// An exception class which represents that the inner version of Protobuild
        /// that was spawned was actually self-invoked, and that the self-invoked version
        /// of Protobuild returned an exit code.
        /// </summary>
        public class SelfInvokeExitException : Exception
        {
            /// <summary>
            /// The exit code of the self-invoked process.
            /// </summary>
            public int ExitCode { get; private set; }

            /// <summary>
            /// Creates a new <see cref="SelfInvokeExitException"/>.  This is for internal use only.
            /// </summary>
            /// <param name="exitCode"></param>
            public SelfInvokeExitException(int exitCode)
            {
                this.ExitCode = exitCode;
            }
        }

        /// <summary>
        /// Calculates the SHA1 hash of a Protobuild executable at a given location.  This
        /// is used so that Protobuild knows if it can self-invoke instead of spawning a new
        /// process when the executable code is identical.
        /// </summary>
        /// <param name="path">The path to the Protobuild executable to check.</param>
        /// <returns>The SHA1 hash.</returns>
        public static string GetProgramHash(string path = null)
        {
            if (path == null)
            {
                path = Assembly.GetEntryAssembly().Location;
            }

            using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var sha1 = new SHA1Managed();
                return BitConverter.ToString(sha1.ComputeHash(reader)).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}