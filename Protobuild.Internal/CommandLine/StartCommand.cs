using System;
using System.IO;

namespace Protobuild
{
    internal class StartCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;
        private readonly IPackageManager m_PackageManager;
        private readonly IPackageUrlParser _packageUrlParser;

        public StartCommand(IActionDispatch actionDispatch, IPackageManager packageManager,
            IPackageUrlParser packageUrlParser)
        {
            this.m_ActionDispatch = actionDispatch;
            this.m_PackageManager = packageManager;
            _packageUrlParser = packageUrlParser;
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
            if (File.Exists(Path.Combine(execution.WorkingDirectory, "Build", "Module.xml")))
            {
                throw new InvalidOperationException("This directory already has a module setup.");
            }

            var package = _packageUrlParser.Parse(execution.StartProjectTemplateURL);
            package.Folder = string.Empty;

            // If no project name is specified, use the name of the current directory.
            if (string.IsNullOrWhiteSpace(execution.StartProjectName))
            {
                var dir = new DirectoryInfo(execution.WorkingDirectory);
                execution.StartProjectName = dir.Name;
                RedirectableConsole.WriteLine("Using current directory name '" + dir.Name + "' as name of new module.");
            }

            // The module can not be loaded before this point because it doesn't
            // yet exist.
            this.m_PackageManager.Resolve(execution.WorkingDirectory, null, package, "Template", execution.StartProjectName, false, false, execution.SafePackageResolution);

            if (execution.DisableProjectGeneration)
            {
                RedirectableConsole.WriteLine("Module has been initialized.");
                return 0;
            }

            RedirectableConsole.WriteLine("Module has been initialized.  Performing --generate to create projects.");

            var module = ModuleInfo.Load(Path.Combine(execution.WorkingDirectory, "Build", "Module.xml"));
            return this.m_ActionDispatch.PerformAction(
                execution.WorkingDirectory,
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

