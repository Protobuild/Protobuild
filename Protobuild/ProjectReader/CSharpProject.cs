using System.Collections.Generic;
using System.Xml;
using System.Xml.Xsl;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;

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
                    "Protobuild.ProjectReader.LineariseProject.xslt")))
            {
                transform.Load(
                    reader,
                    XsltSettings.TrustedXslt,
                    resolver
                );
            }
            var result = new XDocument();
            using (var reader = XmlReader.Create(filename))
            {
                using (var writer = result.CreateWriter())
                {
                    transform.Transform(reader, writer);
                }
            }
            
            // Load the data.
            this.References = (from node in result.Descendants()
                               where node.Name == "Reference"
                               select node.Value).ToList();
            this.Elements = (from node in result.Descendants()
                             where node.Name == "Included"
                             select node.Elements().First().ToXmlElement()).ToList();
        }
        
        public static CSharpProject Load(string filename)
        {
            return new CSharpProject(filename);
        }
    }
}

