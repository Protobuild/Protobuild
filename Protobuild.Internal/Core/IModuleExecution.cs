using System;

namespace Protobuild
{
    internal interface IModuleExecution
    {
        Tuple<int, string, string> RunProtobuild(ModuleInfo module, string args, bool capture = false);
    }
}

