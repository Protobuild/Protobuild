using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
            
            // Run Protobuild in batch mode in each of the submodules
            // where it is present.
            foreach (var submodule in module.GetSubmodules())
                submodule.RunProtobuild("-sync " + this.Platform);
            
            Actions.Sync(module, this.Platform);
            
            this.Log.LogMessage(
                "Synchronisation complete.");

            return true;
        }
    }
}

