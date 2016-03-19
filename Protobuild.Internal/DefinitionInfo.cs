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

        /// <summary>
        /// Whether this project definition should be skipped when the autopackager runs.
        /// </summary>
        public bool SkipAutopackage
        {
            get;
            set;
        }

        /// <summary>
        /// Whether this project definition is a post-build hook.
        /// </summary>
        public bool PostBuildHook { get; set; }

        /// <summary>
        /// Whether this project definition is a standard project (is not an external, include or content project).
        /// </summary>
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

                    if (doc.Root.Attributes().Any(x => x.Name == "Path"))
                    {
                        def.RelativePath = doc.Root.Attribute(XName.Get("Path")).Value;
                    }
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
            catch (System.Xml.XmlException)
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
