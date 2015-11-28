using System.IO;
using System.Linq;

namespace Protobuild
{
    using System;

    public class SyncProjectsTask : BaseTask
    {
        public string SourcePath
        {
            get;
            set;
        }

        public string RootPath
        {
            get;
            set;
        }

        public string Platform
        {
            get;
            set;
        }

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
            foreach (var submodule in module.GetSubmodules(this.Platform))
            {
                if (submodule.HasProtobuildFeature("skip-synchronisation-on-no-standard-projects"))
                {
                    if (submodule.GetDefinitionsRecursively(this.Platform).All(x => !x.IsStandardProject))
                    {
                        // Do not invoke this submodule.
                        this.LogMessage(
                            "Skipping submodule synchronisation for " + submodule.Name + " (there are no projects to synchronise)");
                        continue;
                    }
                }

                this.LogMessage(
                    "Invoking submodule synchronisation for " + submodule.Name);
                submodule.RunProtobuild("-sync " + this.Platform);
                this.LogMessage(
                    "Finished submodule synchronisation for " + submodule.Name);
            }

            var definitions = module.GetDefinitions();
            foreach (var definition in definitions)
            {
                if (definition.Type == "External" || definition.Type == "Content" || definition.RelativePath == null)
                {
                    continue;
                }

                // Read the project file in.
                var path = Path.Combine(module.Path, definition.RelativePath, definition.Name + "." + this.Platform + ".csproj");
                if (File.Exists(path))
                {
                    this.LogMessage("Synchronising: " + definition.Name);
                    var project = CSharpProject.Load(path);
                    var synchroniser = new DefinitionSynchroniser(module, definition, project);
                    synchroniser.Synchronise(this.Platform);
                }
            }
            
            this.LogMessage(
                "Synchronisation complete.");

            return true;
        }
    }
}

