using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Protobuild.Legacy
{
    /// <summary>
    /// This class exists because older versions of Protobuild used to serialize 
    /// the ModuleInfo class into XML.  As we change the newer format, we want to make
    /// sure we can still load really old Module.xml files.
    /// </summary>
    [Serializable]
    public class ModuleInfo
    {
        /// <summary>
        /// The root path of this module.
        /// </summary>
        [NonSerialized]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute(
            "Microsoft.StyleCop.CSharp.Maintainability",
            "SA1401:FieldsMustBePrivate",
            Justification = "This must be a field to allow usage of the NonSerialized attribute.")]
        public string Path;

        public ModuleInfo()
        {
            this.ModuleAssemblies = new string[0];
            this.DefaultAction = "resync";
            this.GenerateNuGetRepositories = true;
        }
        
        public string Name { get; set; }
        
        public string DefaultAction { get; set; }
        
        public string DefaultWindowsPlatforms { get; set; }
        
        public string DefaultMacOSPlatforms { get; set; }
        
        public string DefaultLinuxPlatforms { get; set; }
        
        public bool GenerateNuGetRepositories { get; set; }
        
        public string SupportedPlatforms { get; set; }
        
        public bool? DisableSynchronisation { get; set; }
        
        public string[] ModuleAssemblies { get; set; }
        
        public string DefaultStartupProject { get; set; }
        
        public List<PackageRef> Packages { get; set; }

        private string[] featureCache;
    }

    public struct PackageRef
    {
        public string Uri { get; set; }

        public string GitRef { get; set; }

        public string Folder { get; set; }
    }
}
