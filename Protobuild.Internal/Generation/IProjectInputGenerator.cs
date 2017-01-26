﻿using System;
using System.Xml;
using System.Collections.Generic;
using Protobuild.Services;

namespace Protobuild
{
    internal interface IProjectInputGenerator
    {
        XmlDocument Generate(
            List<XmlDocument> definitions,
            string workingDirectory,
            string rootPath,
            string projectName,
            string platformName,
            string packagesPath,
            IEnumerable<XmlElement> properties,
            List<Service> services);
    }
}

