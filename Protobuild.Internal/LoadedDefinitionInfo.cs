using System.Xml;

namespace Protobuild
{
    internal class LoadedDefinitionInfo
    {
        public XmlDocument Project { get; set; }

        public DefinitionInfo Definition { get; set; }
    }
}