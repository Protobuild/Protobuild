using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.IO;

namespace Protobuild
{
    public class SyncProjectsTask : Task
    {
        [Required]
        public string SourcePath
        {
            get;
            set;
        }

        [Required]
        public string RootPath
        {
            get;
            set;
        }

        [Required]
        public string Platform
        {
            get;
            set;
        }

        [Required]
        public string ModuleName
        {
            get;
            set;
        }
        
        public override bool Execute()
        {
            this.Log.LogMessage(
                "Synchronising projects for " + this.Platform);

            var module = ModuleInfo.Load(Path.Combine(this.RootPath, "Build", "Module.xml"));
            Actions.Sync(module);
            
            this.Log.LogMessage(
                "Synchronisation complete.");

            return true;
        }
    }
}

