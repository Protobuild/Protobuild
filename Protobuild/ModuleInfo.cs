using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Protobuild
{
    [Serializable]
    public class ModuleInfo
    {
        public string Name
        {
            get;
            set;
        }

        public string DefaultAction { get; set; }

        public string DefaultWindowsPlatforms { get; set; }

        public string DefaultMacOSPlatforms { get; set; }

        public string DefaultLinuxPlatforms { get; set; } 

        public bool GenerateNuGetRepositories { get; set; }

        public string SupportedPlatforms { get; set; } 

        public bool? DisableSynchronisation { get; set; }

        public string[] ModuleAssemblies
        {
            get;
            set;
        }
        
        public ModuleInfo()
        {
            this.ModuleAssemblies = new string[0];
            this.DefaultAction = "resync";
            this.GenerateNuGetRepositories = true;
        }
        
        [NonSerialized]
        public string Path;
        
        public BaseTemplate[] GetTemplates()
        {
            return (from assembly in this.ModuleAssemblies
                    let loaded = Assembly.LoadFile(assembly)
                    from type in loaded.GetTypes()
                    where !type.IsAbstract
                    where type.GetConstructor(Type.EmptyTypes) != null
                    select Activator.CreateInstance(type) as BaseTemplate).ToArray();
        }
        
        public DefinitionInfo[] GetDefinitions()
        {
            var result = new List<DefinitionInfo>();
            var path = System.IO.Path.Combine(Path, "Build", "Projects");
            foreach (var file in new DirectoryInfo(path).GetFiles("*.definition"))
            {
                result.Add(DefinitionInfo.Load(file.FullName));
            }
            return result.ToArray();
        }
        
        public IEnumerable<DefinitionInfo> GetDefinitionsRecursively(string relative = "")
        {
            foreach (var definition in this.GetDefinitions())
            {
                definition.Path = (relative + '\\' + definition.Path).Trim('\\');
                definition.ModulePath = this.Path;
                yield return definition;
            }
            foreach (var submodule in this.GetSubmodules())
                foreach (var definition in submodule.GetDefinitionsRecursively((relative + '\\' + submodule.Name).Trim('\\')))
                    yield return definition;
        }
        
        public ModuleInfo[] GetSubmodules()
        {
            var modules = new List<ModuleInfo>();
            foreach (var directory in new DirectoryInfo(Path).GetDirectories())
            {
                var build = directory.GetDirectories().FirstOrDefault(x => x.Name == "Build");
                if (build == null)
                    continue;
                var module = build.GetFiles().FirstOrDefault(x => x.Name == "Module.xml");
                if (module == null)
                    continue;
                modules.Add(ModuleInfo.Load(module.FullName));
            }
            return modules.ToArray();
        }
        
        public static ModuleInfo Load(string xmlFile)
        {
            var serializer = new XmlSerializer(typeof(ModuleInfo));
            var reader = new StreamReader(xmlFile);
            var module = (ModuleInfo)serializer.Deserialize(reader);
            module.Path = new FileInfo(xmlFile).Directory.Parent.FullName;
            reader.Close();
            return module;
        }
        
        public void Save(string xmlFile)
        {
            var serializer = new XmlSerializer(typeof(ModuleInfo));
            var writer = new StreamWriter(xmlFile);
            serializer.Serialize(writer, this);
            writer.Close();
        }
        
        public void RunProtobuild(string args)
        {
            var protobuildPath = System.IO.Path.Combine(this.Path, "Protobuild.exe");
            if (File.Exists(protobuildPath))
            {
                var pi = new ProcessStartInfo
                {
                    FileName = protobuildPath,
                    Arguments = args,
                    WorkingDirectory = this.Path
                };
                var p = Process.Start(pi);
                p.WaitForExit();
            }
        }

        public string NormalizePlatform(string platform)
        {
            var supportedPlatforms = "Android,iOS,Linux,MacOS,Ouya,PSMobile,Windows,Windows8,WindowsGL,WindowsPhone";
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

