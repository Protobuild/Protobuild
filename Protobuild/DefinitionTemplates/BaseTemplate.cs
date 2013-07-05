using System;
using System.IO;
using System.Reflection;
using System.Linq;

namespace Protobuild
{
    public abstract class BaseTemplate
    {
        public abstract string Type { get; }
        
        public abstract void WriteDefinitionFile(string name, Stream output);
        public abstract void CreateFiles(string name, string projectRoot);
        
        public static BaseTemplate GetTemplateForType(string type)
        {
            return (from concreteType in Assembly.GetExecutingAssembly().GetTypes()
                    where !concreteType.IsAbstract
                    where typeof(BaseTemplate).IsAssignableFrom(concreteType)
                    let i = Activator.CreateInstance(concreteType) as BaseTemplate
                    where i.Type == type
                    select i).FirstOrDefault();
        }
    }
}

