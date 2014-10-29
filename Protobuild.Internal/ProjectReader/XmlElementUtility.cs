using System.Xml;
using System.Xml.Linq;

namespace Protobuild
{
    public static class XmlElementUtility
    {
        public static XmlElement ToXmlElement(this XElement el)
        {
            var doc = new XmlDocument();
            using (var reader = el.CreateReader())
            {
                doc.Load(reader);
            }
            return doc.DocumentElement;
        }
    }
}

