// assembly mscorlib
// assembly System
// assembly System.Core
// assembly System.Web
// assembly Microsoft.CSharp
// using System

using System;

public class GenerationFunctions
{
    // **begin**

    private Func<string, string> _getKnownToolCached;

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

    public string StripLeadingDotPaths(string path)
    {
        var components = path.Replace('\\', '/').Split('/');
        var a = 0;
        for (var i = 0; i < components.Length; i++)
        {
            if (components[i] == "." || components[i] == "..")
            {
                continue;
            }

            a = i;
            break;
        }
        return System.Linq.Enumerable.Aggregate(System.Linq.Enumerable.Skip(components, a),
            (x, b) => x + "/" + b);
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
        return System.IO.File.Exists("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/XamMac.dll") ||
			System.IO.File.Exists("/Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Mac/Xamarin.Mac.CSharp.targets");
    }

	public bool HasXamarinMacUnifiedAPI()
	{
		return System.IO.File.Exists("/Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Mac/Xamarin.Mac.CSharp.targets");
	}

	public bool DoesNotHaveXamarinMacUnifiedAPI()
	{
		return !System.IO.File.Exists("/Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Mac/Xamarin.Mac.CSharp.targets");
	}

    public bool CodesignKeyExists()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        var path = System.IO.Path.Combine(home, ".codesignkey");
        return System.IO.File.Exists(path);
    }

    public string GetCodesignKey()
    {
        var home = Environment.GetEnvironmentVariable("HOME");
        if (string.IsNullOrEmpty(home))
        {
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
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
        var components = name.Split('\\', '/');
        if (components.Length == 0)
        {
            throw new Exception("No name specified for NativeBinary");
        }
        return components[components.Length - 1];
    }

    public string ProgramFilesx86()
    {
        if (IntPtr.Size == 8 ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITW6432")))
        {
            return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        }
        return Environment.GetEnvironmentVariable("ProgramFiles");
    }

    public string DetectWindowsCPlusPlusToolsetVersion()
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

    public string DetectWindowsCPlusPlusBuildToolsVersion()
    {
        var platformTools = DetectWindowsCPlusPlusToolsetVersion();
        switch (platformTools)
        {
            case "10":
                return "4";
            default:
                return platformTools;
        }
    }

    public string GetLatestSupportedMSBuildToolsetVersionForPlatform(string hostPlatform, string targetPlatform)
    {
        // Welcome to hell.
        if (hostPlatform == "Windows")
        {
            // Find latest version of MSBuild.
            Microsoft.Win32.RegistryKey registryKey;
            try
            {
                registryKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE")
                    .OpenSubKey("Microsoft")
                    .OpenSubKey("MSBuild")
                    .OpenSubKey("ToolsVersions");
            }
            catch (System.Security.SecurityException)
            {
                registryKey = null;
            }
            catch (NullReferenceException)
            {
                registryKey = null;
            }
            if (registryKey == null)
            {
                Console.Error.WriteLine(
                    "WARNING: No versions of MSBuild were available " +
                    "according to the registry (or they were not readable).  " +
                    "Defaulting to ToolsVersion 4.0.");
                return "4.0";
            }

            var subkeys = registryKey.GetSubKeyNames();
            var orderedVersions = System.Linq.Enumerable.ToList(
                System.Linq.Enumerable.OrderByDescending(subkeys,
                    x => int.Parse(System.Linq.Enumerable.First(x.Split('.')), System.Globalization.CultureInfo.InvariantCulture)));

            if (orderedVersions.Count == 0)
            {
                Console.Error.WriteLine(
                    "WARNING: No versions of MSBuild were available " +
                    "according to the registry (or they were not readable).  " +
                    "Defaulting to ToolsVersion 4.0.");
                return "4.0";
            }

            return System.Linq.Enumerable.First(orderedVersions);
        }
        else
        {
            // MacOS and Linux are always 12.0 in the latest xbuild.  We don't support older
            // versions of xbuild because this implies an older version of the Mono runtime.
            // Since even slightly out-of-date versions of Mono can contain critical bugs, we
            // just tell users to upgrade their version of Mono.
            return "12.0";
        }
    }

    public string GetKnownTool(string toolName)
    {
        if (_getKnownToolCached != null)
        {
            var result = _getKnownToolCached(toolName);
            if (result == null)
            {
                throw new InvalidOperationException("Unable to find tool '" + toolName + "', but it is required for project generation to complete.");
            }
            return result;
        }
        var assembly =
            System.Linq.Enumerable.First(AppDomain.CurrentDomain.GetAssemblies(), x => x.FullName.Contains("Protobuild.Internal"));
        var type = assembly.GetType("Protobuild.LightweightKernel");

        dynamic kernel = Activator.CreateInstance(type);
        kernel.BindAll();

        dynamic knownToolProvider = kernel.Get(assembly.GetType("Protobuild.IKnownToolProvider"));
        _getKnownToolCached = s => knownToolProvider.GetToolExecutablePath(s);
        var result2 = _getKnownToolCached(toolName);
        if (result2 == null)
        {
            throw new InvalidOperationException("Unable to find tool '" + toolName + "', but it is required for project generation to complete.");
        }
        return result2;
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
    public string GetProjectExtension(string language, string platform)
    {
        if (language == "C++")
        {
            if (platform == "Windows")
            {
                return ".vcxproj";
            }
            return ".cproj";
        }
        return ".csproj";
    }

    public bool ProjectIsActive(string platformString, string activePlatform)
    {
        if (string.IsNullOrEmpty(platformString))
        {
            return true;
        }
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
        string activeServicesString)
    {
        if (string.IsNullOrEmpty(serviceString))
        {
            return true;
        }
        var activeServices = activeServicesString.Split(',');
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

    public string ToBase64StringUTF16LE(string input)
    {
        return Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(input));
    }
   
    public string WarnForConcretePCLUsage(string platform)
    {
        if (platform == "PCL")
        {
            return string.Empty;
        }

        Console.WriteLine(
          "WARNING: This project is being built as a PCL (portable class " + 
          "library) for the purpose of concrete code implementation, even " +
          "though the current platform is '" + platform + "'.  Portable " +
          "class library support may not be installed by default " +
          "on end-user machines and this may result in run-time errors or " +
          "crashes when executing the code.  If you encounter compile-time " +
          "errors when building the solution, ensure you have PCL support " +
          "installed on your development machine.  Protobuild STRONGLY " +
          "ADVISES THAT YOU DO NOT USE PORTABLE CLASS LIBRARIES, as their " +
          "support is not guarenteed on less-tested platforms.");
        return string.Empty;
    }

    public string WarnForPostBuildHooksOnOldMacPlatform()
    {
        Console.WriteLine(
            "WARNING: Post-build hooks will NOT work in this project under " +
            "mdtool or Xamarin Studio as it is targeting an old version of " +
            "Mac APIs.  For post-build hooks to run, you must build the " +
            "project with xbuild (but you can't use Mac APIs if you do), " +
            "or have a licensed version of Xamarin Studio and use the Unified " +
            "Mac APIs (the default if Xamarin Studio is installed).");
        return string.Empty;
    }

    // **end**
}
