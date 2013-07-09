using System;
using System.IO;
using System.Reflection;
using System.Linq;
using Gdk;

namespace Protobuild
{
    public abstract class BaseTemplate
    {
        public abstract string Type { get; }
        
        public abstract void WriteDefinitionFile(string name, Stream output);
        public abstract void CreateFiles(string name, string projectRoot);
        
        public virtual void StartNewProject(ModuleInfo module, string name)
        {
        }
        
        public virtual void FinalizeNewProject(ModuleInfo module, DefinitionInfo definition)
        {
        }
        
        public virtual Pixbuf GetIcon()
        {
            return null;
        }
        
        public static BaseTemplate GetTemplateForType(string type)
        {
            return (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from concreteType in assembly.GetTypes()
                    where !concreteType.IsAbstract
                    where typeof(BaseTemplate).IsAssignableFrom(concreteType)
                    let i = Activator.CreateInstance(concreteType) as BaseTemplate
                    where i.Type == type
                    select i).FirstOrDefault();
        }
    }
}

