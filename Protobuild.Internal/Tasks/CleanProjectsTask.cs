using System.IO;
using System.Linq;
using System.Xml;

namespace Protobuild.Tasks
{
    public class CleanProjectsTask : BaseTask
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
                "Starting clean of projects for " + this.Platform);

            var module = ModuleInfo.Load(Path.Combine(this.RootPath, "Build", "Module.xml"));
            var definitions = module.GetDefinitionsRecursively(this.Platform).ToArray();
            
            // Run Protobuild in batch mode in each of the submodules
            // where it is present.
            foreach (var submodule in module.GetSubmodules(Platform))
                submodule.RunProtobuild("-clean " + Platform);
                
            foreach (var definition in definitions.Select(x => x.Name))
            {
                this.LogMessage("Cleaning: " + definition);
                var projectDoc = new XmlDocument();
                projectDoc.Load(Path.Combine(
                    this.SourcePath,
                    definition + ".definition"));
                if (projectDoc == null ||
                    projectDoc.DocumentElement.Name != "Project")
                    continue;
                var path = Path.Combine(
                    this.RootPath,
                    projectDoc.DocumentElement.Attributes["Path"].Value
                        .Replace('\\', Path.DirectorySeparatorChar)
                        .Replace('/', Path.DirectorySeparatorChar),
                    projectDoc.DocumentElement.Attributes["Name"].Value + "." +
                    this.Platform + ".csproj");
                if (File.Exists(path))
                    File.Delete(path);
            }
            
            var solution = Path.Combine(
                this.RootPath,
                this.ModuleName + "." + this.Platform + ".sln");
            this.LogMessage("Cleaning: (solution)");
            if (File.Exists(solution))
                File.Delete(solution);

            this.LogMessage(
                "Clean complete.");

            return true;
        }
    }
}

