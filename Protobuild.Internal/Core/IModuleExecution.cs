using System;

namespace Protobuild
{
    public interface IModuleExecution
    {
        Tuple<int, string, string> RunProtobuild(ModuleInfo module, string args, bool capture = false);
    }
}

