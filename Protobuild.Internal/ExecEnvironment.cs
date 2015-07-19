using System;

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
    }
}