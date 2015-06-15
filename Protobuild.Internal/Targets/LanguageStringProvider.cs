using System;

namespace Protobuild
{
    public class LanguageStringProvider : ILanguageStringProvider
    {
        public string GetFileSuffix(Language language, string platform)
        {
            switch (language)
            {
                case Language.CSharp:
                    return "CSharp";
                case Language.CPlusPlus:
                    switch (platform)
                    {
                        case "Windows":
                            return "CPlusPlus.VisualStudio";
                        default:
                            return "CPlusPlus.MonoDevelop";
                    }
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
                case Language.CPlusPlus:
                    return "C++";
                default:
                    throw new NotSupportedException();
            }
        }

        public Language GetLanguageFromConfigurationName(string language)
        {
            switch (language)
            {
                case "C#":
                    return Language.CSharp;
                case "C++":
                    return Language.CPlusPlus;
                default:
                    throw new NotSupportedException();
            }
        }

        public string GetProjectExtension(Language language, string platform)
        {
            switch (language)
            {
                case Language.CSharp:
                    return "csproj";
                case Language.CPlusPlus:
                    if (platform == "Windows")
                    {
                        return "vcxproj";
                    }
                    else
                    {
                        // TODO: Work out what the project extension is for MonoDevelop
                        return "ccproj";
                    }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

