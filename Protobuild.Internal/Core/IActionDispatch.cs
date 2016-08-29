using System;

namespace Protobuild
{
    internal interface IActionDispatch
    {
        bool PerformAction(
            ModuleInfo module, 
            string action, 
            string platform, 
            string[] enabledServices, 
            string[] disabledServices, 
            string serviceSpecPath,
            bool debugServiceResolution,
            bool disablePackageResolution,
            bool disableHostPlatformGeneration,
            bool? taskParallelisation,
            bool? safeResolve);

        bool DefaultAction(
            ModuleInfo module, 
            string platform, 
            string[] enabledServices, 
            string[] disabledServices, 
            string serviceSpecPath,
            bool debugServiceResolution,
            bool disablePackageResolution,
            bool disableHostPlatformGeneration,
            bool? taskParallelisation,
            bool? safeResolve);
    }
}

