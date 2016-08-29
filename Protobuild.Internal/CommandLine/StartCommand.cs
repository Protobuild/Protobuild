using System;
using System.IO;

namespace Protobuild
{
    internal class StartCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;
        private readonly IPackageManager m_PackageManager;

        public StartCommand(IActionDispatch actionDispatch, IPackageManager packageManager)
        {
            this.m_ActionDispatch = actionDispatch;
            this.m_PackageManager = packageManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the template's URL.");
            }

            pendingExecution.StartProjectTemplateURL = args[0];
            pendingExecution.StartProjectName = args.Length >= 2 ? args[1] : null;
        }

        public int Execute(Execution execution)
        {
            if (File.Exists(Path.Combine("Build", "Module.xml")))
            {
                throw new InvalidOperationException("This directory already has a module setup.");
            }

            var url = execution.StartProjectTemplateURL;
            var branch = "master";
            if (url.LastIndexOf('@') > url.LastIndexOf('/'))
            {
                // A branch / commit ref is specified.
                branch = url.Substring(url.LastIndexOf('@') + 1);
                url = url.Substring(0, url.LastIndexOf('@'));
            }

            var packageRef = new PackageRef
            {
                Uri = url,
                GitRef = branch,
                Folder = string.Empty
            };

            // If no project name is specified, use the name of the current directory.
            if (string.IsNullOrWhiteSpace(execution.StartProjectName))
            {
                var dir = new DirectoryInfo(Environment.CurrentDirectory);
                execution.StartProjectName = dir.Name;
                Console.WriteLine("Using current directory name '" + dir.Name + "' as name of new module.");
            }

            // The module can not be loaded before this point because it doesn't
            // yet exist.
            this.m_PackageManager.Resolve(null, packageRef, "Template", execution.StartProjectName, false, false, execution.SafePackageResolution);

            if (execution.DisableProjectGeneration)
            {
                Console.WriteLine("Module has been initialized.");
                return 0;
            }

            Console.WriteLine("Module has been initialized.  Performing --generate to create projects.");

            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
            return this.m_ActionDispatch.PerformAction(
                module,
                "generate",
                execution.Platform,
                execution.EnabledServices.ToArray(),
                execution.DisabledServices.ToArray(),
                execution.ServiceSpecificationPath,
                execution.DebugServiceResolution,
                execution.DisablePackageResolution,
                execution.DisableHostProjectGeneration,
                execution.UseTaskParallelisation,
                execution.SafePackageResolution)
                ? 0 : 1;
        }

        public string GetDescription()
        {
            return @"
Download the template from the specified URL and use the specified name
for the project.  This command will only run if there is no module initialized
in the current directory.
";
        }

        public int GetArgCount()
        {
            return 2;
        }

        public string[] GetArgNames()
        {
            return new[] { "template_url", "name" };
        }

        public bool IsInternal()
        {
            return false;
        }

        public bool IsRecognised()
        {
            return true;
        }

        public bool IsIgnored()
        {
            return false;
        }
    }
}

