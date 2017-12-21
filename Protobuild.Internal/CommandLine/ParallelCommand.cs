using System;

namespace Protobuild
{
    internal class ParallelCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.UseTaskParallelisation = true;
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetShortCategory()
        {
            return "Internal use";
        }

        public string GetShortDescription()
        {
            return "enable parallelism (on by default on Windows, off by default on macOS/Linux)";
        }

        public string GetDescription()
        {
            return @"
Enables task parallelisation internally.  On Windows task parallelisation is
enabled by default, on Mac and Linux task parallelisation is disabled by default because
older versions of Mono may not support threads properly.  To enable task parallelisation
on Mac and Linux, use --parallel.  To disable task parallelisation on Windows, use
--no-parallel.
";
        }

        public int GetArgCount()
        {
            return 0;
        }

        public string[] GetShortArgNames()
        {
            return GetArgNames();
        }

        public string[] GetArgNames()
        {
            return new string[0];
        }

        public bool IsInternal()
        {
            return false;
        }

        public bool IsRecognised()
        {
            return true;
        }

        public bool IsIgnored()
        {
            return false;
        }
    }
}
