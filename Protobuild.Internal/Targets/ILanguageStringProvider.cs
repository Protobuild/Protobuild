using System;

namespace Protobuild
{
    public interface ILanguageStringProvider
    {
        string GetFileSuffix(Language language);

        string GetConfigurationName(Language language);

        Language GetLanguageFromConfigurationName(string language);

        string GetProjectExtension(Language language);
    }
}

