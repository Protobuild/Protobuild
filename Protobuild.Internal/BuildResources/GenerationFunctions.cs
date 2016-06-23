// assembly mscorlib
// assembly System
// assembly System.Core
// assembly System.Web
// assembly Microsoft.CSharp
// using System
// using System.Text

using System;
using System.Linq;
using System.Text;

/// <summary>
/// The C# functions available to the XSLT generation files.
/// </summary>
public class GenerationFunctions
{
    // **begin**

    private Func<string, string> _getKnownToolCached;

    /// <summary>
    /// Normalizes the filename of an XAP file (used on the WindowsPhone platform).
    /// </summary>
    /// <param name="origName">The original filename.</param>
    /// <returns>The normalized filename.</returns>
    public string NormalizeXAPName(string origName)
    {
        return origName.Replace('.', '_');
    }
    
    /// <summary>
    /// Calculates a relative path from one absolute directory to another absolute directory.
    /// </summary>
    /// <param name="from">The absolute directory to calculate from.</param>
    /// <param name="to">The absolute directory that is the target.</param>
    /// <returns>A relative path from one directory to another.</returns>
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

    /// <summary>
    /// Generates a GUID based on a string value.
    /// </summary>
    /// <param name="source">The source value to generate a GUID from.</param>
    /// <returns>The generated GUID.</returns>
    public string GenerateGuid(string source)
    {
        if (String.IsNullOrEmpty(source))
            return Guid.Empty.ToString();
        var guidBytes = new byte[16];
        for (var i = 0; i < guidBytes.Length; i++)
            guidBytes[i] = (byte)0;
        var nameBytes = Encoding.ASCII.GetBytes(source);
        unchecked
        {
            for (var i = 0; i < nameBytes.Length; i++)
                guidBytes[i % 16] += nameBytes[i];
            for (var i = nameBytes.Length; i < 16; i++)
                guidBytes[i] += nameBytes[i % nameBytes.Length];
        }
        var guid = new Guid(guidBytes);
        return guid.ToString();
    }

    /// <summary>
    /// Strips leading dot paths from a relative path, e.g. the "./" and "../" entries.
    /// </summary>
    /// <param name="path">The path to strip leading dots from.</param>
    /// <returns>The path with leading dot paths stripped.</returns>
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

    /// <summary>
    /// Returns whether or not the platform and service strings are active given the
    /// current state of the generation and the item being generated.
    /// </summary>
    /// <param name="platformString">The &lt;Platform&gt; tag of the item.</param>
    /// <param name="includePlatformString">The &lt;IncludePlatform&gt; tag of the item.</param>
    /// <param name="excludePlatformString">The &lt;ExcludePlatform&gt; tag of the item.</param>
    /// <param name="serviceString">The &lt;Service&gt; tag of the item.</param>
    /// <param name="includeServiceString">The &lt;IncludeService&gt; tag of the item.</param>
    /// <param name="excludeServiceString">The &lt;ExcludeService&gt; tag of the item.</param>
    /// <param name="activePlatform">The current active platform being generated for.</param>
    /// <param name="activeServicesString">The current active service string for generation.</param>
    /// <returns>Whether this item should be included in the output of the generation.</returns>
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

    /// <summary>
    /// Returns whether or not the platform is active given the
    /// current state of the generation and the item being generated.
    /// </summary>
    /// <param name="platformString">The &lt;Platform&gt; tag of the item.</param>
    /// <param name="includePlatformString">The &lt;IncludePlatform&gt; tag of the item.</param>
    /// <param name="excludePlatformString">The &lt;ExcludePlatform&gt; tag of the item.</param>
    /// <param name="activePlatform">The current active platform being generated for.</param>
    /// <returns>Whether this item should be included in the output of the generation.</returns>
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

    /// <summary>
    /// Returns whether or not the service strings are active given the
    /// current state of the generation and the item being generated.
    /// </summary>
    /// <param name="serviceString">The &lt;Service&gt; tag of the item.</param>
    /// <param name="includeServiceString">The &lt;IncludeService&gt; tag of the item.</param>
    /// <param name="excludeServiceString">The &lt;ExcludeService&gt; tag of the item.</param>
    /// <param name="activeServicesString">The current active service string for generation.</param>
    /// <returns>Whether this item should be included in the output of the generation.</returns>
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

    /// <summary>
    /// Returns whether the normalized text value represents a truth value (with the default being false).
    /// </summary>
    /// <param name="text">The text to evaluate.</param>
    /// <returns>Whether the normalized text value represents a truth value.</returns>
    public bool IsTrue(string text)
    {
        return text.ToLower() == "true";
    }

    /// <summary>
    /// Returns whether the normalized text value represents a truth value (with the default being true).
    /// </summary>
    /// <param name="text">The text to evaluate.</param>
    /// <returns>Whether the normalized text value represents a truth value.</returns>
    public bool IsTrueDefault(string text)
    {
        return text.ToLower() != "false";
    }

    /// <summary>
    /// Reads a text file at the given path and returns it's contents.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <returns>The contents of the file.</returns>
    public string ReadFile(string path)
    {
        path = path.Replace('/', System.IO.Path.DirectorySeparatorChar);
        path = path.Replace('\\', System.IO.Path.DirectorySeparatorChar);

        using (var reader = new System.IO.StreamReader(path))
        {
            return reader.ReadToEnd();
        }
    }

    /// <summary>
    /// Returns whether or not the user has any version of Xamarin Mac (Unified or Classic) installed.
    /// </summary>
    /// <returns>Whether or not the user has any version of Xamarin Mac (Unified or Classic) installed.</returns>
    public bool HasXamarinMac()
    {
        return System.IO.File.Exists("/Library/Frameworks/Xamarin.Mac.framework/Versions/Current/lib/mono/XamMac.dll") ||
			System.IO.File.Exists("/Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Mac/Xamarin.Mac.CSharp.targets");
    }

    /// <summary>
    /// Returns whether or not the user has the Unified Xamarin Mac API installed.
    /// </summary>
    /// <returns>Whether or not the user has the Unified Xamarin Mac API installed.</returns>
    public bool HasXamarinMacUnifiedAPI()
	{
		return System.IO.File.Exists("/Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Mac/Xamarin.Mac.CSharp.targets");
	}

    /// <summary>
    /// Returns whether or not the user does not have the Unified Xamarin Mac API installed.
    /// </summary>
    /// <returns>Whether or not the user does not have the Unified Xamarin Mac API installed.</returns>
    public bool DoesNotHaveXamarinMacUnifiedAPI()
	{
		return !System.IO.File.Exists("/Library/Frameworks/Mono.framework/External/xbuild/Xamarin/Mac/Xamarin.Mac.CSharp.targets");
    }

    /// <summary>
    /// Returns whether a file exists at a given path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>Whether a file exists at the given path.</returns>
    public bool FileExists(string path)
    {
        return System.IO.File.Exists(path);
    }

    /// <summary>
    /// Returns whether a code signing key file exists.
    /// </summary>
    /// <returns>Whether a code signing key file exists.</returns>
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

    /// <summary>
    /// Returns the contents of the code signing key file.
    /// </summary>
    /// <returns>The contents of the code signing key file.</returns>
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

    /// <summary>
    /// Calculates the defines to set on the C# project based on a list of defines to add
    /// and a list of defines to remove.
    /// </summary>
    /// <param name="addDefines">The semicolon separated list of defines to add.</param>
    /// <param name="removeDefines">The semicolon separated list of defines to remove.</param>
    /// <returns>The final calculated list of defines.</returns>
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

    /// <summary>
    /// Calculates the filename from a path name.
    /// </summary>
    /// <param name="name">The path to calculate the filename from.</param>
    /// <returns>The filename.</returns>
    public string GetFilename(string name)
    {
        var components = name.Split('\\', '/');
        if (components.Length == 0)
        {
            throw new Exception("No name specified for NativeBinary");
        }
        return components[components.Length - 1];
    }

    /// <summary>
    /// Returns the path to the "Program Files (x86)" directory on 64-bit Windows, and "Program Files" on 32-bit Windows.
    /// </summary>
    /// <returns>The path to the "Program Files (x86)" directory on 64-bit Windows, and "Program Files" on 32-bit Windows.</returns>
    public string ProgramFilesx86()
    {
        if (IntPtr.Size == 8 ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITW6432")))
        {
            return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        }
        return Environment.GetEnvironmentVariable("ProgramFiles");
    }

    /// <summary>
    /// Detects the C++ toolset version that's installed on this Windows machine.
    /// </summary>
    /// <returns>The C++ toolset version that's installed on this Windows machine.</returns>
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

    /// <summary>
    /// Detects the C++ build tools version that's installed on this Windows machine.
    /// </summary>
    /// <returns>The C++ build tools version that's installed on this Windows machine.</returns>
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

    /// <summary>
    /// Detects the latest version of the MSBuild project format that's supported on this host platform for the
    /// given target platform.
    /// </summary>
    /// <param name="hostPlatform">The host platform.</param>
    /// <param name="targetPlatform">The target platform.</param>
    /// <returns>The latest MSBuild toolset version.</returns>
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

    /// <summary>
    /// Detects the installed Windows 10 SDK version.
    /// </summary>
    /// <returns>The installed Windows 10 SDK version.</returns>
    public string DetectWindows10InstalledSDK()
    {
        string versionString;
        Microsoft.Win32.RegistryKey registryKey;
        try
        {
            registryKey = Microsoft.Win32.RegistryKey.OpenBaseKey(
                    Microsoft.Win32.RegistryHive.LocalMachine,
                    Microsoft.Win32.RegistryView.Registry32)
                .OpenSubKey("SOFTWARE")
                .OpenSubKey("Microsoft")
                .OpenSubKey("Microsoft SDKs")
                .OpenSubKey("Windows")
                .OpenSubKey("v10.0");
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
                "WARNING: No versions of the Windows 10 SDK were available " +
                "according to the registry (or they were not readable).  " +
                "Defaulting to ProductVersion 10.0.10240.");
            versionString = "10.0.10240";
        }
        else
        {
            var productVersion = registryKey.GetValue("ProductVersion") as string;
            if (productVersion == null)
            {
                Console.Error.WriteLine(
                    "WARNING: No versions of the Windows 10 SDK were available " +
                    "according to the registry (or they were not readable).  " +
                    "Defaulting to ProductVersion 10.0.10240.");
                versionString = "10.0.10240";
            }
            else
            {
                versionString = productVersion;
            }
        }

        // Try and parse it as a version, ensuring that we append ".0" on the end if necessary.
        try
        {
            var version = Version.Parse(versionString);
            if (version.Revision == -1)
            {
                // No revision component (missing ".0").  Append it.
                return version + ".0";
            }
            return version.ToString();
        }
        catch (Exception)
        {
            // Unable to parse; return it as-is.
            return versionString;
        }
    }

    /// <summary>
    /// Gets the path to the known tool like JSIL, installing it if it's not already installed.
    /// </summary>
    /// <param name="toolName">The known tool name.</param>
    /// <returns>The path to the known tool executable.</returns>
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

    /// <summary>
    /// Returns whether the given path ends in the given extension.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="ext">The file extension.</param>
    /// <returns>Whether the given path ends in the given extension.</returns>
    public bool PathEndsWith(string path, string ext)
    {
        return path.EndsWith(ext);
    }

    /// <summary>
    /// Strips the file extension from the path.
    /// </summary>
    /// <param name="path">A path with a file extension.</param>
    /// <returns>The path with the file extension stripped.</returns>
    public string StripExtension(string path)
    {
        var extl = path.LastIndexOf('.');
        return path.Substring(0, extl);
    }
    
    /// <summary>
    /// Calculates the project filename extension for the target language
    /// and host platform.
    /// </summary>
    /// <remarks>
    /// This implementation should be the same as the implementation
    /// offered by ILanguageStringProvider.
    /// </remarks>
    /// <param name="language">The target language.</param>
    /// <param name="platform">The host platform.</param>
    /// <returns>
    /// The project filename extension for the target language
    /// and host platform.
    /// </returns>
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

    /// <summary>
    /// Returns whether or not the platform is active given the
    /// current state of the generation and the item being generated.
    /// </summary>
    /// <param name="platformString">The &lt;Platform&gt; tag of the item.</param>
    /// <param name="activePlatform">The current active platform being generated for.</param>
    /// <returns>Whether this item should be included in the output of the generation.</returns>
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

    /// <summary>
    /// Returns whether or not the service strings are active given the
    /// current state of the generation and the item being generated.
    /// </summary>
    /// <param name="serviceString">The &lt;Service&gt; tag of the item.</param>
    /// <param name="activeServicesString">The current active service string for generation.</param>
    /// <returns>Whether this item should be included in the output of the generation.</returns>
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

    /// <summary>
    /// Converts the input string to a base64-encoded, UTF16 (little endian) format.  This is
    /// used to encode commands for PowerShell execution.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <returns>The encoded string.</returns>
    public string ToBase64StringUTF16LE(string input)
    {
        return Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(input));
    }
   
    /// <summary>
    /// Emits a warning to the console when concrete PCLs are being used.
    /// </summary>
    /// <param name="platform">The current target platform.</param>
    /// <returns>An empty string.</returns>
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

    /// <summary>
    /// Emits a warning to the console when post-build hooks won't work for
    /// this target platform.
    /// </summary>
    /// <returns>An empty string.</returns>
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
