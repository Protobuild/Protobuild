using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace Protobuild
{
    public static class ExecEnvironment
    {
        public static bool RunProtobuildInProcess = false;

        public static bool DoNotWrapExecutionInTry = false;

        public static int SelfInvokeCounter = 0;

        public static int InvokeSelf(string[] args)
        {
            SelfInvokeCounter++;
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
                SelfInvokeCounter--;
            }
        }

        public static void Exit(int exitCode)
        {
            if (SelfInvokeCounter == 0)
            {
                Environment.Exit(exitCode);
            }
            else
            {
                throw new SelfInvokeExitException(exitCode);
            }
        }

        public class SelfInvokeExitException : Exception
        {
            public int ExitCode { get; private set; }

            public SelfInvokeExitException(int exitCode)
            {
                this.ExitCode = exitCode;
            }
        }

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