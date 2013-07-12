using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Protobuild.Tasks
{
    public class GenerateProjectsTask : Task
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
            try
            {
                this.Log.LogMessage(
                    "Starting generation of projects for " + this.Platform);
    
                var module = ModuleInfo.Load(Path.Combine(this.RootPath, "Build", "Module.xml"));
                var definitions = module.GetDefinitionsRecursively().ToArray();
                
                // Run Protobuild in batch mode in each of the submodules
                // where it is present.
                foreach (var submodule in module.GetSubmodules())
                {
                    this.Log.LogMessage(
                        "Invoking submodule generation for " + submodule.Name);
                    submodule.RunProtobuild("-generate " + Platform);
                    this.Log.LogMessage(
                        "Finished submodule generation for " + submodule.Name);
                }
                
                var generator = new ProjectGenerator(
                    this.RootPath,
                    this.Platform,
                    this.Log);
                foreach (var definition in definitions)
                {
                    this.Log.LogMessage("Loading: " + definition.Name);
                    generator.Load(Path.Combine(
                        definition.ModulePath,
                        "Build",
                        "Projects",
                        definition.Name + ".definition"),
                        module.Path,
                        definition.ModulePath);
                }
                foreach (var definition in definitions.Where(x => x.ModulePath == module.Path))
                {
                    this.Log.LogMessage("Generating: " + definition.Name);
                    generator.Generate(definition.Name);
                }
    
                var solution = Path.Combine(
                    this.RootPath,
                    this.ModuleName + "." + this.Platform + ".sln");
                this.Log.LogMessage("Generating: (solution)");
                generator.GenerateSolution(solution);
    
                this.Log.LogMessage(
                    "Generation complete.");
            }
            catch (Exception ex)
            {
                this.Log.LogErrorFromException(ex);
                this.Log.LogError(ex.StackTrace);
            }
            
            return true;
        }
    }
}

