// assembly System.Core
// assembly System.Web
// assembly Microsoft.CSharp
// using System
// using System.Linq
// using System.Web

public class GenerationFunctions
{
    using System;
    using System.Linq;
    using System.Web;

    // **begin**

    public string NormalizeXAPName(string origName)
    {
        return origName.Replace('.', '_');
    }

    public string GetRelativePath(string from, string to)
    {
        try
        {
            var current = Environment.CurrentDirectory;
            from = System.IO.Path.Combine(current, from.Replace('\\', '/'));
            to = System.IO.Path.Combine(current, to.Replace('\\', '/'));
            return (new Uri(from).MakeRelativeUri(new Uri(to)))
                .ToString().Replace('/', '\\');
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public bool ProjectAndServiceIsActive(
        string platformString,
        string includePlatformString,
        string excludePlatformString,
        string serviceString,
        string includeServiceString,
        string excludeServiceString,
        string activePlatform,
        string activeServicesString)
    {
        if (!ProjectIsActive(platformString, includePlatformString, excludePlatformString, activePlatform))
        {
            return false;
        }

        return ServiceIsActive(serviceString, includeServiceString, excludeServiceString, activeServicesString);
    }

    public bool ProjectIsActive(
        string platformString,
        string includePlatformString,
        string excludePlatformString,
        string activePlatform)
    {
        // Choose either <Platforms> or <IncludePlatforms>
        if (string.IsNullOrEmpty(platformString))
        {
            platformString = includePlatformString;
        }

        // If the exclude string is set, then we must check this first.
        if (!string.IsNullOrEmpty(excludePlatformString))
        {
            var excludePlatforms = excludePlatformString.Split(',');
            foreach (var i in excludePlatforms)
            {
                if (i == activePlatform)
                {
                    // This platform is excluded.
                    return false;
                }
            }
        }

        // If the platform string is empty at this point, then we allow
        // all platforms since there's no whitelist of platforms configured.
        if (string.IsNullOrEmpty(platformString))
        {
            return true;
        }

        // Otherwise ensure the platform is in the include list.
        var platforms = platformString.Split(',');
        foreach (var i in platforms)
        {
            if (i == activePlatform)
            {
                return true;
            }
        }

        return false;
    }

    public bool ServiceIsActive(
        string serviceString,
        string includeServiceString,
        string excludeServiceString,
        string activeServicesString)
    {
        var activeServices = activeServicesString.Split(',');

        // Choose either <Services> or <IncludeServices>
        if (string.IsNullOrEmpty(serviceString))
        {
            serviceString = includeServiceString;
        }

        // If the exclude string is set, then we must check this first.
        if (!string.IsNullOrEmpty(excludeServiceString))
        {
            var excludeServices = excludeServiceString.Split(',');
            foreach (var i in excludeServices)
            {
                if (System.Linq.Enumerable.Contains(activeServices, i))
                {
                    // This service is excluded.
                    return false;
                }
            }
        }

        // If the service string is empty at this point, then we allow
        // all services since there's no whitelist of services configured.
        if (string.IsNullOrEmpty(serviceString))
        {
            return true;
        }

        // Otherwise ensure the service is in the include list.
        var services = serviceString.Split(',');
        foreach (var i in services)
        {
            if (System.Linq.Enumerable.Contains(activeServices, i))
            {
                return true;
            }
        }

        return false;
    }

    public bool IsTrue(string text)
    {
        return text.ToLower() == "true";
    }

    public bool IsTrueDefault(string text)
    {
        return text.ToLower() != "false";
    }

    public string ReadFile(string path)
    {
        path = path.Replace('/', System.IO.Path.DirectorySeparatorChar);
        path = path.Replace('\\', System.IO.Path.DirectorySeparatorChar);

        using (var reader = new System.IO.StreamReader(path))
        {
            return reader.ReadToEnd();
        }
    }

    public bool HasXamarinMac()
    {
        return System.IO.File.Exists("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/XamMac.dll");
    }

    public bool CodesignKeyExists()
    {
        var home = System.Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        }
        var path = System.IO.Path.Combine(home, ".codesignkey");
        return System.IO.File.Exists(path);
    }

    public string GetCodesignKey()
    {
        var home = System.Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            home = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        }
        var path = System.IO.Path.Combine(home, ".codesignkey");
        using (var reader = new System.IO.StreamReader(path))
        {
            return reader.ReadToEnd().Trim();
        }
    }

    public string CalculateDefines(string addDefines, string removeDefines)
    {
        var addArray = addDefines.Trim(';').Split(';');
        var removeArray = removeDefines.Trim(';').Split(';');

        var list = new System.Collections.Generic.List<string>();
        foreach (var a in addArray)
        {
            if (!list.Contains(a))
            {
                list.Add(a);
            }
        }
        foreach (var r in removeArray)
        {
            if (list.Contains(r))
            {
                list.Remove(r);
            }
        }

        return string.Join(";", list.ToArray());
    }

    public string GetFilename(string name)
    {
        var components = name.Split(new[] {'\\', '/'});
        if (components.Length == 0)
        {
            throw new Exception("No name specified for NativeBinary");
        }
        return components[components.Length - 1];
    }

    public string ProgramFilesx86()
    {
        if (IntPtr.Size == 8 ||
            !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("PROCESSOR_ARCHITW6432")))
        {
            return System.Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        }
        return System.Environment.GetEnvironmentVariable("ProgramFiles");
    }

    public string DetectPlatformToolsetVersion()
    {
        // We can't just select a common low version here; we need to pick a
        // toolset version that's installed with Visual Studio.
        var x86programfiles = ProgramFilesx86();
        for (var i = 20; i >= 10; i--)
        {
            var path = System.IO.Path.Combine(x86programfiles, @"Microsoft Visual Studio " + i + @".0\VC");
            if (System.IO.Directory.Exists(path))
            {
                return i.ToString();
            }
        }
        return "10";
    }

    public string DetectBuildToolsVersion()
    {
        var platformTools = DetectPlatformToolsetVersion();
        switch (platformTools)
        {
            case "10":
                return "4";
            default:
                return platformTools;
        }
    }

    private Func<string, string> _getKnownToolCached;

    public string GetKnownTool(string toolName)
    {
        if (_getKnownToolCached != null)
        {
            return _getKnownToolCached(toolName);
        }
        var assembly =
            System.AppDomain.CurrentDomain.GetAssemblies().First(x => x.FullName.Contains("Protobuild.Internal"));
        var type = assembly.GetType("Protobuild.LightweightKernel");

        dynamic kernel = Activator.CreateInstance(type);
        kernel.BindCore();
        kernel.BindBuildResources();
        kernel.BindGeneration();
        kernel.BindJSIL();
        kernel.BindTargets();
        kernel.BindFileFilter();
        kernel.BindPackages();

        dynamic knownToolProvider = kernel.Get(assembly.GetType("Protobuild.IKnownToolProvider"));
        _getKnownToolCached = s => knownToolProvider.GetToolExecutablePath(s);
        return _getKnownToolCached(toolName);
    }

    public bool PathEndsWith(string path, string ext)
    {
      return path.EndsWith(ext);
    }

    public string StripExtension(string path)
    {
      var extl = path.LastIndexOf('.');
      return path.Substring(0, extl);
    }

    // This implementation should be the same as the implementation
    // offered by ILanguageStringProvider.
    public string GetProjectExtension(string language, string platform) {
      if (language == "C++") {
        if (platform == "Windows") {
          return ".vcxproj";
        } else {
          return ".cproj";
        }
      } else {
        return ".csproj";
      }
    }

    // **end**
}