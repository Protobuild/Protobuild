using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace Protobuild
{
    public class CSharpProject
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public string Type { get; set; }
        public List<string> References { get; set; }
        public List<XmlElement> Elements { get; set; }
    
        private CSharpProject(string filename)
        {
            var resolver = new EmbeddedResourceResolver();
            var transform = new XslCompiledTransform();
            using (var reader = XmlReader.Create(
                Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "LineariseProject.xslt")))
            {
                transform.Load(
                    reader,
                    XsltSettings.TrustedXslt,
                    resolver
                );
            }
            XDocument result;
            using (var memory = new MemoryStream())
            {
                using (var reader = XmlReader.Create(filename))
                {
                    using (var writer = XmlWriter.Create(memory))
                    {
                        transform.Transform(reader, writer);
                    }
                }
                memory.Seek(0, SeekOrigin.Begin);
                using (var reader = XmlReader.Create(memory))
                {
                    result = XDocument.Load(reader);
                }
            }
            
            // Load the data.
            this.References = (from node in result.Descendants()
                               where node.Name.LocalName == "Reference"
                               select node.Value).ToList();
            this.Elements = (from node in result.Descendants()
                             where node.Name.LocalName == "Included"
                             select node.Elements().First().ToXmlElement()).ToList();
        }
        
        public static CSharpProject Load(string filename)
        {
            return new CSharpProject(filename);
        }
    }
}

