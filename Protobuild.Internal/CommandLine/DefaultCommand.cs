using System;
using System.IO;

namespace Protobuild
{
    internal class DefaultCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;

        private readonly IKnownToolProvider _knownToolProvider;

        private readonly ExecuteCommand _executeCommand;
        private readonly IAutomatedBuildController _automatedBuildController;

        public DefaultCommand(IActionDispatch actionDispatch, IKnownToolProvider knownToolProvider, ExecuteCommand executeCommand, IAutomatedBuildController automatedBuildController)
        {
            this.m_ActionDispatch = actionDispatch;
            _knownToolProvider = knownToolProvider;
            _executeCommand = executeCommand;
            _automatedBuildController = automatedBuildController;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            throw new NotSupportedException();
        }

        public int Execute(Execution execution)
        {
            if (!File.Exists(Path.Combine("Build", "Module.xml")))
            {
                _knownToolProvider.GetToolExecutablePath("Protobuild.Manager");

                var subexecution = new Execution();
                subexecution.ExecuteProjectName = "Protobuild.Manager";
                subexecution.ExecuteProjectArguments = new string[0];

                return _executeCommand.Execute(subexecution);
            }

            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
            if (module.DefaultAction == "automated-build")
            {
                return _automatedBuildController.Execute("automated.build");
            }

            return this.m_ActionDispatch.DefaultAction(
                module,
                enabledServices: execution.EnabledServices.ToArray(),
                disabledServices: execution.DisabledServices.ToArray(),
                serviceSpecPath: execution.ServiceSpecificationPath,
                debugServiceResolution: execution.DebugServiceResolution,
                disablePackageResolution: execution.DisablePackageResolution,
                disableHostPlatformGeneration: execution.DisableHostProjectGeneration,
                taskParallelisation: execution.UseTaskParallelisation) ? 0 : 1;
        }

        public string GetDescription()
        {
            throw new NotSupportedException();
        }

        public int GetArgCount()
        {
            throw new NotSupportedException();
        }

        public string[] GetArgNames()
        {
            throw new NotSupportedException();
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

