using System;

namespace Protobuild
{
    public static class ExecEnvironment
    {
        public static bool RunProtobuildInProcess = false;

        public static int SelfInvokeCounter = 0;

        public static void InvokeSelf(string[] args)
        {
            SelfInvokeCounter++;
            try
            {
                MainClass.Main(args);
            }
            catch (SelfInvokeExitException)
            {
            }
            SelfInvokeCounter--;
        }

        public static void Exit(int exitCode)
        {
            if (SelfInvokeCounter == 0)
            {
                Environment.Exit(exitCode);
            }
            else
            {
                throw new SelfInvokeExitException();
            }
        }

        public class SelfInvokeExitException : Exception
        {
        }
    }
}