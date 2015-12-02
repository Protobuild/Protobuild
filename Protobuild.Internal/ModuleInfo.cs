//-----------------------------------------------------------------------
// <copyright file="ModuleInfo.cs" company="Protobuild Project">
// The MIT License (MIT)
// 
// Copyright (c) Various Authors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//     The above copyright notice and this permission notice shall be included in
//     all copies or substantial portions of the Software.
// 
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//     THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System.Xml;
using System.Xml.Linq;

namespace Protobuild
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Serialization;

    /// <summary>
    /// Represents a Protobuild module.
    /// </summary>
    public class ModuleInfo
    {
        private DefinitionInfo[] _cachedDefinitions;

        private string[] _cachedFeatures;

        private readonly Dictionary<string, ModuleInfo[]> _cachedSubmodules;

        private readonly Dictionary<string, DefinitionInfo[]> _cachedRecursivedDefinitions;

        /// <summary>
        /// Initializes a new instance of the <see cref="Protobuild.ModuleInfo"/> class.
        /// </summary>
        public ModuleInfo()
        {
            this.DefaultAction = "resync";
            this.GenerateNuGetRepositories = true;

            _cachedSubmodules = new Dictionary<string, ModuleInfo[]>();
            _cachedRecursivedDefinitions = new Dictionary<string, DefinitionInfo[]>();
        }

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        /// <value>The module name.</value>
        public string Name { get; set; }

        /// <summary>
        /// The root path of this module.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the default action to be taken when Protobuild runs in the module.
        /// </summary>
        /// <value>The default action that Protobuild will take.</value>
        public string DefaultAction { get; set; }

        /// <summary>
        /// Gets or sets a comma seperated list of default platforms when the host platform is Windows.
        /// </summary>
        /// <value>The comma seperated list of default platforms when the host platform is Windows.</value>
        public string DefaultWindowsPlatforms { get; set; }

        /// <summary>
        /// Gets or sets a comma seperated list of default platforms when the host platform is Mac OS.
        /// </summary>
        /// <value>The comma seperated list of default platforms when the host platform is Mac OS.</value>
        public string DefaultMacOSPlatforms { get; set; }

        /// <summary>
        /// Gets or sets a comma seperated list of default platforms when the host platform is Linux.
        /// </summary>
        /// <value>The comma seperated list of default platforms when the host platform is Linux.</value>
        public string DefaultLinuxPlatforms { get; set; } 

        /// <summary>
        /// Gets or sets a value indicating whether the NuGet repositories.config file is generated.
        /// </summary>
        /// <value><c>true</c> if the NuGet repositories.config file is generated; otherwise, <c>false</c>.</value>
        public bool GenerateNuGetRepositories { get; set; }

        /// <summary>
        /// Gets or sets a comma seperated list of supported platforms in this module.
        /// </summary>
        /// <value>The comma seperated list of supported platforms.</value>
        public string SupportedPlatforms { get; set; } 

        /// <summary>
        /// Gets or sets a value indicating whether synchronisation is completely disabled in this module.
        /// </summary>
        /// <value><c>true</c> if synchronisation is disabled and will be skipped; otherwise, <c>false</c>.</value>
        public bool? DisableSynchronisation { get; set; }

        /// <summary>
        /// Gets or sets the name of the default startup project.
        /// </summary>
        /// <value>The name of the default startup project.</value>
        public string DefaultStartupProject { get; set; } 

        /// <summary>
        /// Gets or sets a set of package references.
        /// </summary>
        /// <value>The registered packages.</value>
        public List<PackageRef> Packages { get; set; }

        /// <summary>
        /// Loads the Protobuild module from the Module.xml file.
        /// </summary>
        /// <param name="xmlFile">The path to a Module.xml file.</param>
        /// <returns>The loaded Protobuild module.</returns>
        public static ModuleInfo Load(string xmlFile)
        {
            var def = new ModuleInfo();
            var doc = XDocument.Load(xmlFile);

            var xsi = doc.Root == null ? null : doc.Root.Attribute(XName.Get("xsi", "http://www.w3.org/2000/xmlns/"));
            if (xsi != null && xsi.Value == "http://www.w3.org/2001/XMLSchema-instance")
            {
                // This is a previous module info format.
                var serializer = new XmlSerializer(typeof(ModuleInfo));
                var reader = new StreamReader(xmlFile);
                var module = (ModuleInfo)serializer.Deserialize(reader);
                module.Path = new FileInfo(xmlFile).Directory.Parent.FullName;
                reader.Close();

                // Re-save in the new format.
                if (xmlFile == System.IO.Path.Combine("Build", "Module.xml"))
                {
                    module.Save(xmlFile);
                }

                return module;
            }

            Func<string, string> getStringValue = name =>
            {
                if (doc.Root == null)
                {
                    return null;
                }

                var elem = doc.Root.Element(XName.Get(name));
                if (elem == null)
                {
                    return null;
                }

                return elem.Value;
            };
            
            def.Name = getStringValue("Name");
            def.Path = new FileInfo(xmlFile).Directory.Parent.FullName;
            def.DefaultAction = getStringValue("DefaultAction");
            def.DefaultLinuxPlatforms = getStringValue("DefaultLinuxPlatforms");
            def.DefaultMacOSPlatforms = getStringValue("DefaultMacOSPlatforms");
            def.DefaultWindowsPlatforms = getStringValue("DefaultWindowsPlatforms");
            def.DefaultStartupProject = getStringValue("DefaultStartupProject");
            def.SupportedPlatforms = getStringValue("SupportedPlatforms");
            def.DisableSynchronisation = getStringValue("DisableSynchronisation") == "true";
            def.GenerateNuGetRepositories = getStringValue("GenerateNuGetRepositories") == "true";
            def.Packages = new List<PackageRef>();

            if (doc.Root != null)
            {
                var packagesElem = doc.Root.Element(XName.Get("Packages"));
                if (packagesElem != null)
                {
                    var packages = packagesElem.Elements();
                    foreach (var package in packages)
                    {
                        var packageRef = new PackageRef
                        {
                            Folder = package.Attribute(XName.Get("Folder")).Value,
                            GitRef = package.Attribute(XName.Get("GitRef")).Value,
                            Uri = package.Attribute(XName.Get("Uri")).Value,
                            Platforms = null,
                        };
                        
                        var platforms = package.Attribute(XName.Get("Platforms"));
                        var platformsArray = platforms?.Value.Split(',');
                        if (platformsArray?.Length > 0)
                        {
                            packageRef.Platforms = platformsArray;
                        }

                        def.Packages.Add(packageRef);
                    }
                }
            }

            return def;
        }

        /// <summary>
        /// Loads all of the project definitions present in the current module.
        /// </summary>
        /// <returns>The loaded project definitions.</returns>
        public DefinitionInfo[] GetDefinitions()
        {
            if (_cachedDefinitions == null)
            {
                var result = new List<DefinitionInfo>();
                var path = System.IO.Path.Combine(this.Path, "Build", "Projects");
                if (!Directory.Exists(path))
                {
                    return new DefinitionInfo[0];
                }
                foreach (var file in new DirectoryInfo(path).GetFiles("*.definition"))
                {
                    result.Add(DefinitionInfo.Load(file.FullName));
                }

                _cachedDefinitions = result.ToArray();
            }

            return _cachedDefinitions;
        }

        /// <summary>
        /// Loads all of the project definitions present in the current module and all submodules.
        /// </summary>
        /// <returns>The loaded project definitions.</returns>
        /// <param name="relative">The current directory being scanned.</param>
        public IEnumerable<DefinitionInfo> GetDefinitionsRecursively(string platform = null, string relative = "")
        {
            if (!_cachedRecursivedDefinitions.ContainsKey(platform ?? "<null>") || relative != "")
            {
                var definitions = new List<DefinitionInfo>();

                foreach (var definition in this.GetDefinitions())
                {
                    definition.AbsolutePath = (this.Path + '\\' + definition.RelativePath).Trim('\\');
                    definition.RelativePath = (relative + '\\' + definition.RelativePath).Trim('\\');
                    definition.ModulePath = this.Path;
                    definitions.Add(definition);
                }

                foreach (var submodule in this.GetSubmodules(platform))
                {
                    var from = this.Path.Replace('\\', '/').TrimEnd('/') + "/";
                    var to = submodule.Path.Replace('\\', '/');
                    var subRelativePath = (new Uri(from).MakeRelativeUri(new Uri(to)))
                        .ToString().Replace('/', '\\');

                    foreach (var definition in submodule.GetDefinitionsRecursively(platform, subRelativePath.Trim('\\'))
                        )
                    {
                        definitions.Add(definition);
                    }
                }

                if (relative == "")
                {
                    _cachedRecursivedDefinitions[platform ?? "<null>"] = definitions.Distinct(new DefinitionEqualityComparer()).ToArray();
                }
                else
                {
                    return definitions.Distinct(new DefinitionEqualityComparer());
                }
            }

            return _cachedRecursivedDefinitions[platform ?? "<null>"];
        }

        private class DefinitionEqualityComparer : IEqualityComparer<DefinitionInfo>
        {
            public bool Equals(DefinitionInfo a, DefinitionInfo b)
            {
                return a.ModulePath == b.ModulePath &&
                    a.Name == b.Name;
            }

            public int GetHashCode(DefinitionInfo obj)
            {
                return obj.ModulePath.GetHashCode() + obj.Name.GetHashCode() * 37;
            }
        }

        /// <summary>
        /// Loads all of the submodules present in this module.
        /// </summary>
        /// <returns>The loaded submodules.</returns>
        public ModuleInfo[] GetSubmodules(string platform = null)
        {
            if (!_cachedSubmodules.ContainsKey(platform ?? "<null>"))
            {
                var modules = new List<ModuleInfo>();
                foreach (var directoryInit in new DirectoryInfo(this.Path).GetDirectories())
                {
                    var directory = directoryInit;

                    if (File.Exists(System.IO.Path.Combine(directory.FullName, ".redirect")))
                    {
                        // This is a redirected submodule (due to package resolution).  Load the
                        // module from it's actual path.
                        using (var reader = new StreamReader(System.IO.Path.Combine(directory.FullName, ".redirect")))
                        {
                            var targetPath = reader.ReadToEnd().Trim();
                            directory = new DirectoryInfo(targetPath);
                        }
                    }

                    var build = directory.GetDirectories().FirstOrDefault(x => x.Name == "Build");
                    if (build == null)
                    {
                        continue;
                    }

                    var module = build.GetFiles().FirstOrDefault(x => x.Name == "Module.xml");
                    if (module == null)
                    {
                        continue;
                    }

                    modules.Add(ModuleInfo.Load(module.FullName));
                }

                if (platform != null)
                {
                    foreach (var directoryInit in new DirectoryInfo(this.Path).GetDirectories())
                    {
                        var directory = directoryInit;

                        if (File.Exists(System.IO.Path.Combine(directory.FullName, ".redirect")))
                        {
                            // This is a redirected submodule (due to package resolution).  Load the
                            // module from it's actual path.
                            using (
                                var reader = new StreamReader(System.IO.Path.Combine(directory.FullName, ".redirect")))
                            {
                                var targetPath = reader.ReadToEnd().Trim();
                                directory = new DirectoryInfo(targetPath);
                            }
                        }

                        var platformDirectory = new DirectoryInfo(System.IO.Path.Combine(directory.FullName, platform));

                        if (!platformDirectory.Exists)
                        {
                            continue;
                        }

                        var build = platformDirectory.GetDirectories().FirstOrDefault(x => x.Name == "Build");
                        if (build == null)
                        {
                            continue;
                        }

                        var module = build.GetFiles().FirstOrDefault(x => x.Name == "Module.xml");
                        if (module == null)
                        {
                            continue;
                        }

                        modules.Add(ModuleInfo.Load(module.FullName));
                    }
                }

                _cachedSubmodules[platform ?? "<null>"] = modules.ToArray();
            }

            return _cachedSubmodules[platform ?? "<null>"];
        }

        /// <summary>
        /// Saves the current module to a Module.xml file.
        /// </summary>
        /// <param name="xmlFile">The path to a Module.xml file.</param>
        public void Save(string xmlFile)
        {
            var doc = new XmlDocument();

            var root = doc.CreateElement("Module");
            doc.AppendChild(root);

            Action<string, string> createStringElement = (name, val) =>
            {
                if (val != null)
                {
                    var e = doc.CreateElement(name);
                    e.InnerText = val;
                    root.AppendChild(e);
                }
            };

            Action<string, bool?> createBooleanElement = (name, val) =>
            {
                if (val != null)
                {
                    var e = doc.CreateElement(name);
                    e.InnerText = val.Value ? "true" : "false";
                    root.AppendChild(e);
                }
            };

            createStringElement("Name", Name);
            createStringElement("DefaultAction", DefaultAction);
            createStringElement("DefaultLinuxPlatforms", DefaultLinuxPlatforms);
            createStringElement("DefaultMacOSPlatforms", DefaultMacOSPlatforms);
            createStringElement("DefaultWindowsPlatforms", DefaultWindowsPlatforms);
            createStringElement("DefaultStartupProject", DefaultStartupProject);
            createStringElement("SupportedPlatforms", SupportedPlatforms);
            createBooleanElement("DisableSynchronisation", DisableSynchronisation);
            createBooleanElement("GenerateNuGetRepositories", GenerateNuGetRepositories);

            if (Packages != null && Packages.Count > 0)
            {
                var elem = doc.CreateElement("Packages");
                root.AppendChild(elem);

                foreach (var package in Packages)
                {
                    var packageElem = doc.CreateElement("Package");
                    packageElem.SetAttribute("Uri", package.Uri);
                    packageElem.SetAttribute("Folder", package.Folder);
                    packageElem.SetAttribute("GitRef", package.GitRef);

                    if (package.Platforms != null && package.Platforms.Length > 0)
                    {
                        packageElem.SetAttribute("Platforms", package.Platforms.Aggregate((a, b) => a + "," + b));
                    }

                    elem.AppendChild(packageElem);
                }
            }

            using (var writer = XmlWriter.Create(xmlFile, new XmlWriterSettings {Indent = true, IndentChars = "  "}))
            {
                doc.Save(writer);
            }
        }

        /// <summary>
        /// Determines whether the instance of Protobuild in this module
        /// has a specified feature.
        /// </summary>
        public bool HasProtobuildFeature(string feature)
        {
            if (_cachedFeatures == null)
            {
                var result = this.RunProtobuild("--query-features", true);
                var exitCode = result.Item1;
                var stdout = result.Item2;

                if (exitCode != 0 || stdout.Contains("Protobuild.exe [options]"))
                {
                    _cachedFeatures = new string[0];
                }

                _cachedFeatures = stdout.Split('\n');
            }

            return _cachedFeatures.Contains(feature);
        }

        /// <summary>
        /// Runs the instance of Protobuild.exe present in the module.
        /// </summary>
        /// <param name="args">The arguments to pass to Protobuild.</param>
        public Tuple<int, string, string> RunProtobuild(string args, bool capture = false)
        {
            var invokeInline = false;
            var invokeIfIdenticalHash = Path == Environment.CurrentDirectory || (!capture && HasProtobuildFeature("inline-invocation-if-identical-hashed-executables"));
            if (invokeIfIdenticalHash)
            {
                var myHash = ExecEnvironment.GetProgramHash();
                var targetHash = ExecEnvironment.GetProgramHash(System.IO.Path.Combine(Path, "Protobuild.exe"));
                if (myHash == targetHash)
                {
                    invokeInline = true;
                }
            }

            if ((ExecEnvironment.RunProtobuildInProcess || invokeInline) && !capture)
            {
                var old = Environment.CurrentDirectory;
                try
                {
                    Environment.CurrentDirectory = this.Path;
                    return new Tuple<int, string, string>(
                        ExecEnvironment.InvokeSelf(args.SplitCommandLine().ToArray()),
                        string.Empty,
                        string.Empty);
                }
                finally
                {
                    Environment.CurrentDirectory = old;
                }
            }

            var protobuildPath = System.IO.Path.Combine(this.Path, "Protobuild.exe");

            try
            {
                var chmodStartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = "a+x Protobuild.exe",
                    WorkingDirectory = this.Path,
                    CreateNoWindow = capture,
                    UseShellExecute = false
                };
                Process.Start(chmodStartInfo);
            }
            catch (ExecEnvironment.SelfInvokeExitException)
            {
                throw;
            }
            catch
            {
            }

            var stdout = string.Empty;
            var stderr = string.Empty;

            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (File.Exists(protobuildPath))
                {
                    var pi = new ProcessStartInfo
                    {
                        FileName = protobuildPath,
                        Arguments = args,
                        WorkingDirectory = this.Path,
                        CreateNoWindow = capture,
                        RedirectStandardError = capture,
                        RedirectStandardInput = capture,
                        RedirectStandardOutput = capture,
                        UseShellExecute = false
                    };
                    var p = new Process { StartInfo = pi };
                    if (capture)
                    {
                        p.OutputDataReceived += (sender, eventArgs) =>
                        {
                            if (!string.IsNullOrEmpty(eventArgs.Data))
                            {
                                if (capture)
                                {
                                    stdout += eventArgs.Data + "\n";
                                }
                                else
                                {
                                    Console.WriteLine(eventArgs.Data);
                                }
                            }
                        };
                        p.ErrorDataReceived += (sender, eventArgs) =>
                        {
                            if (!string.IsNullOrEmpty(eventArgs.Data))
                            {
                                if (capture)
                                {
                                    stderr += eventArgs.Data + "\n";
                                }
                                else
                                {
                                    Console.Error.WriteLine(eventArgs.Data);
                                }
                            }
                        };
                    }
                    try
                    {
                        p.Start();
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        if (ex.Message.Contains("Cannot find the specified file"))
                        {
                            // Mono sometimes throws this error even though the
                            // file does exist on disk.  The best guess is there's
                            // a race condition between performing chmod on the
                            // file and Mono actually seeing it as an executable file.
                            // Show a warning and sleep for a bit before retrying.
                            if (attempt != 2)
                            {
                                Console.WriteLine("WARNING: Unable to execute Protobuild.exe, will retry again...");
                                System.Threading.Thread.Sleep(2000);
                                continue;
                            }
                            else
                            {
                                Console.WriteLine("ERROR: Still unable to execute Protobuild.exe.");
                                throw;
                            }
                        }
                    }
                    if (capture)
                    {
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                    }
                    p.WaitForExit();
                    return new Tuple<int, string, string>(p.ExitCode, stdout, stderr);
                }
            }

            return new Tuple<int, string, string>(1, string.Empty, string.Empty);
        }

        public static string GetSupportedPlatformsDefault()
        {
            return "Android,iOS,tvOS,Linux,MacOS,Ouya,PCL,PSMobile,Windows,Windows8,WindowsGL,WindowsPhone,WindowsPhone81,WindowsUniversal";
        }

        /// <summary>
        /// Normalizes the platform string from user input, automatically correcting case
        /// and validating against a list of supported platforms.
        /// </summary>
        /// <returns>The platform string.</returns>
        /// <param name="platform">The normalized platform string.</param>
        public string NormalizePlatform(string platform)
        {
            var supportedPlatforms = GetSupportedPlatformsDefault();
            var defaultPlatforms = true;

            if (!string.IsNullOrEmpty(this.SupportedPlatforms))
            {
                supportedPlatforms = this.SupportedPlatforms;
                defaultPlatforms = false;
            }

            var supportedPlatformsArray = supportedPlatforms.Split(new[] { ',' })
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            // Search the array to find a platform that matches case insensitively
            // to the specified platform.  If we are using the default list, then we allow
            // other platforms to be specified (in case the developer has modified the XSLT to
            // support others but is not using <SupportedPlatforms>).  If the developer has
            // explicitly set the supported platforms, then we return null if the user passes
            // an unknown platform (the caller is expected to exit at this point).
            foreach (var supportedPlatform in supportedPlatformsArray)
            {
                if (string.Compare(supportedPlatform, platform, true) == 0)
                {
                    return supportedPlatform;
                }
            }

            if (defaultPlatforms)
            {
                return platform;
            }
            else
            {
                return null;
            }
        }
    }
}
