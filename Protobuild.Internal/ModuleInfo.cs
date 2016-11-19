using System.Xml;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using LegacyModuleInfo = Protobuild.Legacy.ModuleInfo;

namespace Protobuild
{
    /// <summary>
    /// Represents a Protobuild module.
    /// </summary>
    public class ModuleInfo
    {
        private DefinitionInfo[] _cachedDefinitions;

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
        /// Gets or sets the authors of the module.
        /// </summary>
        /// <value>The name or names of the authors.</value>
        public string Authors { get; set; }

        /// <summary>
        /// Gets or sets the description of the module.
        /// </summary>
        /// <value>The description of the module.</value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the URL that specifies the license for the module.
        /// </summary>
        /// <value>The license URL for the module.</value>
        public string LicenseUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL for the project.
        /// </summary>
        /// <value>The project URL for the module.</value>
        public string ProjectUrl { get; set; }

        /// <summary>
        /// Gets or sets a 64x64 icon for the module.
        /// </summary>
        /// <value>The URL to the 64x64 icon.</value>
        public string IconUrl { get; set; }

        /// <summary>
        /// Gets or sets the semantic version of the module.
        /// </summary>
        /// <value>The semantic version of the module.</value>
        public string SemanticVersion { get; set; }

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
        /// Gets or sets the feature set to use when Protobuild is executing for
        /// this module.  If this value is null, use the full feature set.
        /// </summary>
        /// <value>The feature set.</value>
        public List<Feature> FeatureSet { get; set; }

        /// <summary>
        /// Gets or sets the list of cached features.  You shouldn't access this
        /// property directly, instead use the <see cref="IFeatureManager.IsFeatureEnabledInSubmodule"/>
        /// method.
        /// </summary>
        /// <value>The cached features.</value>
        public Feature[] CachedInternalFeatures { get; set; }

        /// <summary>
        /// Loads the module information from an XML stream.
        /// </summary>
        /// <param name="xmlStream">The XML stream.</param>
        /// <param name="modulePath">The virtual path to the module on disk.</param>
        /// <returns>The loaded module.</returns>
        public static ModuleInfo Load(Stream xmlStream, string modulePath)
        {
            return LoadInternal(
                XDocument.Load(xmlStream), 
                modulePath,
                () =>
                {
                    throw new NotSupportedException("Can't fallback with stream argument.");
                });
        }

        /// <summary>
        /// Loads the Protobuild module from the Module.xml file.
        /// </summary>
        /// <param name="xmlFile">The path to a Module.xml file.</param>
        /// <returns>The loaded Protobuild module.</returns>
        public static ModuleInfo Load(string xmlFile)
        {
            var modulePath = new FileInfo(xmlFile).Directory.Parent.FullName;
            return LoadInternal(
                XDocument.Load(xmlFile),
                modulePath,
                () =>
                {
                    // This is a previous module info format.
                    var serializer = new XmlSerializer(typeof(LegacyModuleInfo));
                    var reader = new StreamReader(xmlFile);
                    var legacyModule = (LegacyModuleInfo)serializer.Deserialize(reader);
                    legacyModule.Path = new FileInfo(xmlFile).Directory.Parent.FullName;
                    reader.Close();

                    // Migrate from the old format.
                    var module = UpgradeFromOlderFormat(legacyModule, modulePath);

                    // Re-save in the new format.
                    if (xmlFile == System.IO.Path.Combine("Build", "Module.xml"))
                    {
                        module.Save(xmlFile);
                    }

                    return module;
                });
        }

        private static ModuleInfo UpgradeFromOlderFormat(LegacyModuleInfo legacyModule, string modulePath)
        {
            var module = new ModuleInfo();
            module.Name = legacyModule.Name;
            module.Path = modulePath;
            module.DefaultAction = legacyModule.DefaultAction;
            module.DefaultWindowsPlatforms = legacyModule.DefaultWindowsPlatforms;
            module.DefaultMacOSPlatforms = legacyModule.DefaultMacOSPlatforms;
            module.DefaultLinuxPlatforms = legacyModule.DefaultLinuxPlatforms;
            module.GenerateNuGetRepositories = legacyModule.GenerateNuGetRepositories;
            module.SupportedPlatforms = legacyModule.SupportedPlatforms;
            module.DisableSynchronisation = legacyModule.DisableSynchronisation;
            module.DefaultStartupProject = legacyModule.DefaultStartupProject;
            module.Packages = new List<PackageRef>();
            if (legacyModule.Packages != null)
            {
                foreach (var oldPkg in legacyModule.Packages)
                {
                    module.Packages.Add(new PackageRef
                    {
                        Folder = oldPkg.Folder,
                        GitRef = oldPkg.GitRef,
                        Uri = oldPkg.Uri
                    });
                }
            }
            return module;
        }

        private static ModuleInfo LoadInternal(XDocument doc, string modulePath, Func<ModuleInfo> fallback)
        {
            var def = new ModuleInfo();
            var xsi = doc.Root == null ? null : doc.Root.Attribute(XName.Get("xsi", "http://www.w3.org/2000/xmlns/"));
            if (xsi != null && xsi.Value == "http://www.w3.org/2001/XMLSchema-instance")
            {
                return fallback();
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
            def.Authors = getStringValue("Authors");
            def.Description = getStringValue("Description");
            def.ProjectUrl = getStringValue("ProjectUrl");
            def.LicenseUrl = getStringValue("LicenseUrl");
            def.IconUrl = getStringValue("IconUrl");
            def.SemanticVersion = getStringValue("SemanticVersion");
            def.Path = modulePath;
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

                var featureSetElem = doc.Root.Element(XName.Get("FeatureSet"));
                if (featureSetElem != null)
                {
                    def.FeatureSet = new List<Feature>();

                    var features = featureSetElem.Elements();
                    foreach (var feature in features)
                    {
                        try
                        {
                            def.FeatureSet.Add((Feature) Enum.Parse(typeof(Feature), feature.Value));
                        }
                        catch
                        {
                            Console.Error.WriteLine("Unknown feature in Module.xml; ignoring: " + feature.Value);
                        }
                    }
                }
                else
                {
                    def.FeatureSet = null;
                }

                // Check if the feature set is present and if it does not contain
                // the PackageManagement feature.  If that feature isn't there, we
                // ignore any of the data in Packages and just set the value to 
                // an empty list.
                if (def.FeatureSet != null && !def.FeatureSet.Contains(Feature.PackageManagement))
                {
                    def.Packages.Clear();
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
        /// <param name="platform">The target platform.</param>
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
            createStringElement("Authors", Authors);
            createStringElement("Description", Description);
            createStringElement("ProjectUrl", ProjectUrl);
            createStringElement("LicenseUrl", LicenseUrl);
            createStringElement("IconUrl", IconUrl);
            createStringElement("SemanticVersion", SemanticVersion);
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
        /// Returns the default list of supported platforms in Protobuild.
        /// </summary>
        /// <returns>The default list of supported platforms in Protobuild.</returns>
        public static string GetSupportedPlatformsDefault()
        {
            return "Android,iOS,tvOS,Linux,MacOS,Ouya,PCL,PSMobile,Windows,Windows8,WindowsGL,WindowsPhone,WindowsPhone81,WindowsUniversal";
        }
    }
}
