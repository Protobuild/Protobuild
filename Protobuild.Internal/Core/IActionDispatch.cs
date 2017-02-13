using System;

namespace Protobuild
{
    internal interface IActionDispatch
    {
        bool PerformAction(
            string workingDirectory,
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
            bool? safeResolve,
            bool debugProjectGeneration);

        bool DefaultAction(
            string workingDirectory,
            ModuleInfo module, 
            string platform, 
            string[] enabledServices, 
            string[] disabledServices, 
            string serviceSpecPath,
            bool debugServiceResolution,
            bool disablePackageResolution,
            bool disableHostPlatformGeneration,
            bool? taskParallelisation,
            bool? safeResolve,
            bool debugProjectGeneration);
    }
}

