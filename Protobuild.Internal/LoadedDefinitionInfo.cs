using System.Xml;

namespace Protobuild
{
    public class LoadedDefinitionInfo
    {
        public XmlDocument Project { get; set; }

        public DefinitionInfo Definition { get; set; }
    }
}