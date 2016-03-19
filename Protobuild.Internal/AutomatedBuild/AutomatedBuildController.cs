using System;
using System.IO;

namespace Protobuild
{
    internal class AutomatedBuildController : IAutomatedBuildController
    {
        private readonly IAutomatedBuildRuntimeV1 _automatedBuildRuntimeV1;

        public AutomatedBuildController(IAutomatedBuildRuntimeV1 automatedBuildRuntimeV1)
        {
            _automatedBuildRuntimeV1 = automatedBuildRuntimeV1;
        }

        public int Execute(string path)
        {
            IAutomatedBuildRuntime runtime = null;
            string script = null;
            using (var reader = new StreamReader(path))
            {
                script = reader.ReadToEnd();
                if (script.StartsWith("#version 1"))
                {
                    script = script.Substring("#version 1".Length).TrimStart();
                    runtime = _automatedBuildRuntimeV1;
                }
                else
                {
                    throw new InvalidOperationException(
                        "Your automated build script must start with #version N, where " +
                        "N is the number indicating the script runtime version for " +
                        "automated builds.");
                }
            }

            object handle;
            try
            {
                handle = runtime.Parse(script);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                return 1;
            }
            try
            {
                return runtime.Execute(handle);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message + Environment.NewLine + ex.StackTrace);
                return 1;
            }
        }
    }
}