using System;

namespace Protobuild
{
    public class LanguageStringProvider : ILanguageStringProvider
    {
        public string GetFileSuffix(Language language)
        {
            switch (language)
            {
                case Language.CSharp:
                    return "CSharp";
                default:
                    throw new NotSupportedException();
            }
        }

        public string GetConfigurationName(Language language)
        {
            switch (language)
            {
                case Language.CSharp:
                    return "C#";
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

