using System;

namespace Protobuild
{
    internal class LanguageStringProvider : ILanguageStringProvider
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;

        public LanguageStringProvider(IHostPlatformDetector hostPlatformDetector)
        {
            _hostPlatformDetector = hostPlatformDetector;
        }

        public string GetFileSuffix(Language language)
        {
            switch (language)
            {
                case Language.CSharp:
                    return "CSharp";
                case Language.CPlusPlus:
                    if (_hostPlatformDetector.DetectPlatform() == "Windows")
                    {
                        return "CPlusPlus.VisualStudio";
                    } 
                    else
                    {
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

        public string GetProjectExtension(Language language)
        {
            switch (language)
            {
                case Language.CSharp:
                    return "csproj";
                case Language.CPlusPlus:
                if (_hostPlatformDetector.DetectPlatform() == "Windows")
                    {
                        return "vcxproj";
                    }
                    else
                    {
                        return "cproj";
                    }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

