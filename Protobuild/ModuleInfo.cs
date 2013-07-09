using System;
using System.IO;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using System.Reflection;

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
        
        public string[] ModuleAssemblies
        {
            get;
            set;
        }
        
        public ModuleInfo()
        {
            this.ModuleAssemblies = new string[0];
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
    }
}

