namespace Protobuild
{
    public class NuGetPlatformMapping : INuGetPlatformMapping
    {
        public string GetFrameworkNameForWrite(string platform)
        {
            switch (platform)
            {
                case "Android":
                    return "monoandroid";
                case "iOS":
                    return "xamarinios";
                case "tvOS":
                    return "xamarintvos";
                case "Linux":
                    return "mono40";
                case "MacOS":
                    return "xamarinmac";
                case "PCL":
                    return "netstandard1.1";
                case "Windows":
                    return "net45";
                case "Windows8":
                    return "win8";
                case "WindowsPhone":
                    return "wp8";
                case "WindowsPhone81":
                    return "wp81";
                case "WindowsUniversal":
                    return "uap";
            }

            return null;
        }

        public string[] GetFrameworkNamesForRead(string platform)
        {
            switch (platform)
            {
                case "Android":
                    return new[]
                    {
                        "?monoandroid",
                        "?netstandard1.4"
                    };
                case "iOS":
                    return new[]
                    {
                        "?xamarinios",
                        "?monoios",
                        "?netstandard1.4"
                    };
                case "tvOS":
                    return new[]
                    {
                        "?xamarintvos",
                        "?netstandard1.4"
                    };
                case "Linux":
                    return new[]
                    {
                        "=mono40",
                        "=Mono40",
                        "=mono20",
                        "=Mono20",
                        "=net45",
                        "=Net45",
                        "=net40-client",
                        "=Net40-client",
                        "=net403",
                        "=Net403",
                        "=net40",
                        "=Net40",
                        "=net35-client",
                        "=Net35-client",
                        "=net20",
                        "=Net20",
                        "=net11",
                        "=Net11",
                        "=20",
                        "=11",
                        "=",
                        "?mono40",
                        "?Mono40",
                        "?mono20",
                        "?Mono20",
                        "?net45",
                        "?Net45",
                        "?net4",
                        "?Net4",
                    };
                case "MacOS":
                    return new[]
                    {
                        "?xamarinmac",
                        "?monomac",
                        "=mono40",
                        "=Mono40",
                        "=mono20",
                        "=Mono20",
                        "=net45",
                        "=Net45",
                        "=net40-client",
                        "=Net40-client",
                        "=net403",
                        "=Net403",
                        "=net40",
                        "=Net40",
                        "=net35-client",
                        "=Net35-client",
                        "=net20",
                        "=Net20",
                        "=net11",
                        "=Net11",
                        "=20",
                        "=11",
                        "=",
                        "?mono40",
                        "?Mono40",
                        "?mono20",
                        "?Mono20",
                        "?net45",
                        "?Net45",
                        "?net4",
                        "?Net4",
                    };
                case "Ouya":
                    return new[]
                    {
                        "?monoandroid",
                        "?netstandard1.4"
                    };
                case "PCL":
                    return new[]
                    {
                        "=netstandard1.1",
                        "=portable-net45+win8+wpa81",
                        "?netstandard1.1",
                        "?portable-net45+win8+wpa81"
                    };
                case "Windows8":
                    return new[]
                    {
                        "=win8",
                        "=Win8",
                        "?win8",
                        "?Win8",
                    };
                case "WindowsPhone":
                    return new[]
                    {
                        "=wp8",
                        "=Wp8",
                        "?wp8",
                        "?Wp8",
                    };
                case "WindowsPhone81":
                    return new[]
                    {
                        "=wp81",
                        "=Wp81",
                        "?wp81",
                        "?Wp81",
                    };
                case "WindowsUniversal":
                    return new[]
                    {
                        "=uap10.0",
                        "=uap",
                        "=netcore451",
                        "=netcore",
                        "=dotnet",
                        "?uap10",
                        "?uap",
                    };
                case "Windows":
                case "WindowsGL":
                default:
                    return new[]
                    {
                        "=net45",
                        "=Net45",
                        "=net40-client",
                        "=Net40-client",
                        "=net403",
                        "=Net403",
                        "=net40",
                        "=Net40",
                        "=net35-client",
                        "=Net35-client",
                        "=net20",
                        "=Net20",
                        "=net11",
                        "=Net11",
                        "=20",
                        "=11",
                        "=",
                        "?net45",
                        "?Net45",
                        "?net4",
                        "?Net4",
                    };
            }
        }
    }
}
