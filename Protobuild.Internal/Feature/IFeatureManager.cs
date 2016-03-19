using System;

namespace Protobuild
{
    internal interface IFeatureManager
    {
        void LoadFeaturesFromCommandLine(string commandArguments);

        void LoadFeaturesForCurrentDirectory();

        void LoadFeaturesFromSpecificModule(ModuleInfo module);

        void LoadFeaturesFromSpecificModule(string path);

        string GetFeatureArgumentToPassToSubmodule(ModuleInfo module, ModuleInfo submodule);

        bool IsFeatureEnabled(Feature feature);

        bool IsFeatureEnabledInSubmodule(ModuleInfo module, ModuleInfo submodule, Feature feature);

        string[] GetEnabledInternalFeatureIDs();

        void ValidateEnabledFeatures();

        Feature[] GetAllEnabledFeatures();
    }
}
