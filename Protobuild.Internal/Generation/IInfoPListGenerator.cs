using System;
using System.Xml;

namespace Protobuild
{
	public interface IInfoPListGenerator
	{
        void GenerateInfoPListIfNeeded(DefinitionInfo definition, XmlDocument project, string platform);
	}
}

