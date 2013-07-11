//
// This source code is licensed in accordance with the licensing outlined
// on the main Tychaia website (www.tychaia.com).  Changes to the
// license on the website apply retroactively.
//
using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Linq;
using System.Diagnostics;

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
            this.Log.LogMessage(
                "Starting generation of projects for " + this.Platform);

            var module = ModuleInfo.Load(Path.Combine(this.RootPath, "Build", "Module.xml"));
            var definitions = module.GetDefinitions();
            
            // Run Protobuild in batch mode in each of the submodules
            // where it is present.
            foreach (var submodule in module.GetSubmodules())
                submodule.RunProtobuild("-generate");

            var generator = new ProjectGenerator(
                this.RootPath,
                this.Platform,
                this.Log);
            foreach (var definition in definitions.Select(x => x.Name))
            {
                this.Log.LogMessage("Loading: " + definition);
                generator.Load(Path.Combine(
                    this.SourcePath,
                    definition + ".definition"));
            }
            foreach (var definition in definitions.Select(x => x.Name))
            {
                this.Log.LogMessage("Generating: " + definition);
                generator.Generate(definition);
            }

            var solution = Path.Combine(
                this.RootPath,
                this.ModuleName + "." + this.Platform + ".sln");
            this.Log.LogMessage("Generating: (solution)");
            generator.GenerateSolution(solution);

            this.Log.LogMessage(
                "Generation complete.");

            return true;
        }
    }
}

