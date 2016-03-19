using System;

namespace Protobuild
{
    internal interface ILanguageStringProvider
    {
        string GetFileSuffix(Language language);

        string GetConfigurationName(Language language);

        Language GetLanguageFromConfigurationName(string language);

        string GetProjectExtension(Language language);
    }
}

