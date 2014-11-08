using System;

namespace Protobuild
{
    public interface IActionDispatch
    {
        bool PerformAction(
            ModuleInfo module, 
            string action, 
            string platform = null, 
            string[] enabledServices = null, 
            string[] disabledServices = null, 
            string serviceSpecPath = null);

        bool DefaultAction(
            ModuleInfo module, 
            string platform = null, 
            string[] enabledServices = null, 
            string[] disabledServices = null, 
            string serviceSpecPath = null);
    }
}

