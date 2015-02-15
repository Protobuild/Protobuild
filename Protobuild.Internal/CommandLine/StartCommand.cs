using System;
using System.IO;

namespace Protobuild
{
    public class StartCommand : ICommand
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

            if (args.Length < 2 || args[0] == null || args[1] == null)
            {
                throw new InvalidOperationException("You must provide the template's URL and the new module name.");
            }

            pendingExecution.StartProjectTemplateURL = args[0];
            pendingExecution.StartProjectName = args[1];
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

            // The module can not be loaded before this point because it doesn't
            // yet exist.
            this.m_PackageManager.Resolve(null, packageRef, "Template", execution.StartProjectName, false);

            Console.WriteLine("Module has been initialized.  Performing --generate to create projects.");

            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
            return this.m_ActionDispatch.PerformAction(
                module,
                "generate",
                execution.Platform,
                execution.EnabledServices.ToArray(),
                execution.DisabledServices.ToArray(),
                execution.ServiceSpecificationPath)
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
    }
}

