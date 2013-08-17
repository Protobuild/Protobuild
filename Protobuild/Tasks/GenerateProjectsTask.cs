using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Protobuild.Tasks
{
    public class GenerateProjectsTask : BaseTask
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
                "Starting generation of projects for " + this.Platform);

            var module = ModuleInfo.Load(Path.Combine(this.RootPath, "Build", "Module.xml"));
            var definitions = module.GetDefinitionsRecursively().ToArray();
            
            // Run Protobuild in batch mode in each of the submodules
            // where it is present.
            foreach (var submodule in module.GetSubmodules())
            {
                this.LogMessage(
                    "Invoking submodule generation for " + submodule.Name);
                submodule.RunProtobuild("-generate " + Platform);
                this.LogMessage(
                    "Finished submodule generation for " + submodule.Name);
            }
            
            var generator = new ProjectGenerator(
                this.RootPath,
                this.Platform,
                this.Log);
            foreach (var definition in definitions)
            {
                this.LogMessage("Loading: " + definition.Name);
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
                this.LogMessage("Generating: " + definition.Name);
                generator.Generate(definition.Name);
            }

            var solution = Path.Combine(
                this.RootPath,
                this.ModuleName + "." + this.Platform + ".sln");
            this.LogMessage("Generating: (solution)");
            generator.GenerateSolution(solution);

            this.LogMessage(
                "Generation complete.");
            
            return true;
        }
    }
}

