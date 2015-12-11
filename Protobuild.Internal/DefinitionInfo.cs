//-----------------------------------------------------------------------
// <copyright file="DefinitionInfo.cs" company="Protobuild Project">
// The MIT License (MIT)
// 
// Copyright (c) Various Authors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//     The above copyright notice and this permission notice shall be included in
//     all copies or substantial portions of the Software.
// 
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//     THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Protobuild
{
    using System;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Represents a project definition.
    /// </summary>
    public class DefinitionInfo
    {
        /// <summary>
        /// Gets or sets the name of the project.
        /// </summary>
        /// <value>The project name.</value>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the relative path of the project.
        /// </summary>
        /// <value>The relative project path.</value>
        public string RelativePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the absolute path of the project.
        /// </summary>
        /// <value>The absolute project path.</value>
        public string AbsolutePath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the type of the project.
        /// </summary>
        /// <value>The project type.</value>
        public string Type
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the path to the definition file this definition was loaded from.
        /// </summary>
        /// <value>The path to the definition file.</value>
        public string DefinitionPath
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the module path, that is, the root directory of the module.
        /// </summary>
        /// <value>The module path, also known as the module root directory.</value>
        public string ModulePath
        {
            get;
            set;
        }

        public bool SkipAutopackage
        {
            get;
            set;
        }

        public bool PostBuildHook { get; set; }

        public bool IsStandardProject
        {
            get { return Type != "External" && Type != "Content" && Type != "Include"; }
        }

        /// <summary>
        /// Loads a project definition from the specified XML file.
        /// </summary>
        /// <param name="xmlFile">The path of the XML file to load.</param>
        /// <returns>The loaded project definition.</returns>
        public static DefinitionInfo Load(string xmlFile)
        {
            var def = new DefinitionInfo();
            def.DefinitionPath = xmlFile;

            try
            {
                var doc = XDocument.Load(xmlFile);
                if (doc.Root.Attributes().Any(x => x.Name == "Name"))
                {
                    def.Name = doc.Root.Attribute(XName.Get("Name")).Value;
                }

                if (doc.Root.Name == "Project")
                {
                    if (doc.Root.Attributes().Any(x => x.Name == "Path"))
                    {
                        def.RelativePath = doc.Root.Attribute(XName.Get("Path")).Value;
                    }

                    if (doc.Root.Attributes().Any(x => x.Name == "Type"))
                    {
                        def.Type = doc.Root.Attribute(XName.Get("Type")).Value;
                    }
                }
                else if (doc.Root.Name == "ExternalProject")
                {
                    def.Type = "External";
                }
                else if (doc.Root.Name == "IncludeProject")
                {
                    def.Type = "Include";
                }
                else if (doc.Root.Name == "ContentProject")
                {
                    def.Type = "Content";
                }

                if (doc.Root.Attributes().Any(x => x.Name == "SkipAutopackage"))
                {
                    var skipValue = 
                        doc.Root.Attribute(XName.Get("SkipAutopackage")).Value.ToLowerInvariant();
                    def.SkipAutopackage = skipValue == "true";
                }

                if (doc.Root.Attributes().Any(x => x.Name == "PostBuildHook"))
                {
                    var skipValue =
                        doc.Root.Attribute(XName.Get("PostBuildHook")).Value.ToLowerInvariant();
                    def.PostBuildHook = skipValue == "true";
                }

                return def;
            }
            catch (System.Xml.XmlException ex)
            {
                throw new InvalidOperationException(
                    "Encountered an XML exception while loading " + 
                    xmlFile + " as a project definition.  This indicates " +
                    "that the project definition file is badly formed " +
                    "XML, or otherwise has an incorrect structure.");
            }
        }
    }
}
