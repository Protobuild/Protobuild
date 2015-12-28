using System;
using System.Xml;
using System.Collections.Generic;

namespace Protobuild
{
	public interface IPlatformResourcesGenerator
	{
		void GenerateInfoPListIfNeeded(List<LoadedDefinitionInfo> definitions, DefinitionInfo definition, XmlDocument project, string platform);
	}
}
