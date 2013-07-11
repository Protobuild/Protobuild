using System;
using System.Linq;
using System.Xml.Linq;

namespace Protobuild
{
    public class DefinitionInfo
    {
        public string Name
        {
            get;
            set;
        }
        
        public string Path
        {
            get;
            set;
        }
        
        public string Type
        {
            get;
            set;
        }
        
        public Guid Guid
        {
            get;
            set;
        }
        
        public string DefinitionPath
        {
            get;
            private set;
        }
    
        public static DefinitionInfo Load(string xmlFile)
        {
            var def = new DefinitionInfo();
            def.DefinitionPath = xmlFile;
            var doc = XDocument.Load(xmlFile);
            if (doc.Root.Attributes().Any(x => x.Name == "Name"))
                def.Name = doc.Root.Attribute(XName.Get("Name")).Value;
            if (doc.Root.Name == "Project")
            {
                if (doc.Root.Attributes().Any(x => x.Name == "Path"))
                    def.Path = doc.Root.Attribute(XName.Get("Path")).Value;
                if (doc.Root.Attributes().Any(x => x.Name == "Guid"))
                    def.Guid = Guid.Parse(doc.Root.Attribute(XName.Get("Guid")).Value);
                if (doc.Root.Attributes().Any(x => x.Name == "Type"))
                    def.Type = doc.Root.Attribute(XName.Get("Type")).Value;
            }
            else if (doc.Root.Name == "ExternalProject")
            {
                def.Type = "External";
            }
            else if (doc.Root.Name == "ContentProject")
            {
                def.Type = "Content";
            }
            return def;
        }
    }
}

