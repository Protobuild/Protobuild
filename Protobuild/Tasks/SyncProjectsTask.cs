using System.IO;
using Microsoft.Build.Framework;

namespace Protobuild
{
    public class SyncProjectsTask : BaseTask
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
            this.LogMessage(
                "Synchronising projects for " + this.Platform);

            var module = ModuleInfo.Load(Path.Combine(this.RootPath, "Build", "Module.xml"));
            
            // Run Protobuild in batch mode in each of the submodules
            // where it is present.
            foreach (var submodule in module.GetSubmodules())
                submodule.RunProtobuild("-sync " + this.Platform);
            
            var definitions = module.GetDefinitions();
            foreach (var definition in definitions)
            {
                // Read the project file in.
                var path = Path.Combine(module.Path, definition.Name, definition.Name + "." + this.Platform + ".csproj");
                if (File.Exists(path))
                {
                    this.LogMessage("Synchronising: " + definition.Name);
                    var project = CSharpProject.Load(path);
                    var synchroniser = new DefinitionSynchroniser(definition, project);
                    synchroniser.Synchronise(this.Platform);
                }
            }
            
            this.LogMessage(
                "Synchronisation complete.");

            return true;
        }
    }
}

