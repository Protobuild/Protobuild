using System;
using System.Xml;

namespace Protobuild
{
	public interface IPlatformResourcesGenerator
	{
        void GenerateInfoPListIfNeeded(DefinitionInfo definition, XmlDocument project, string platform);
	}
}

