using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Protobuild
{
    public class EmbeddedResourceResolver : XmlUrlResolver
    {
        public override object GetEntity(
            Uri absoluteUri,
            string role,
            Type ofObjectToReturn)
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetManifestResourceStream(
                this.GetType(),
                Path.GetFileName(absoluteUri.AbsolutePath));
        }
    }
}

