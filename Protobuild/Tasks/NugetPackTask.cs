using System.Diagnostics;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Protobuild.Tasks
{
    public class NugetPackTask : Task
    {
        [Required]
        public string RootPath
        {
            get;
            set;
        }
        
        [Required]
        public string ProjectPath
        {
            get;
            set;
        }
        
        [Required]
        public string NuspecFile
        {
            get;
            set;
        }
        
        public override bool Execute()
        {
            // Use NuGet to build a package.
            var nuget = Path.Combine(this.RootPath, "Build", "nuget.exe");
            if (!File.Exists(nuget))
            {
                Log.LogError(
                    "nuget.exe not found.  Run 'Protobuild.exe " +
                    "-extract-util' if it has been deleted.");
            }
            
            var process = new Process();
            process.StartInfo.FileName = nuget;
            process.StartInfo.Arguments = "pack " + this.NuspecFile;
            process.StartInfo.WorkingDirectory = this.ProjectPath;
            process.StartInfo.UseShellExecute = false;
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
    }
}

