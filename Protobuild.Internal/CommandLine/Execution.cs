using System;
using System.Collections.Generic;

namespace Protobuild
{
    internal class Execution
    {
        public Execution()
        {
            this.EnabledServices = new List<string>();
            this.DisabledServices = new List<string>();
            this.BuildProperties = new Dictionary<string, string>();
        }

        public ICommand CommandToExecute { get; set; }

        public string Platform { get; set; }

        public List<string> EnabledServices { get; set; }

        public List<string> DisabledServices { get; set; }

        public string ServiceSpecificationPath { get; set; }

        public string PackageUrl { get; set; }

        public string PackageSourceFolder { get; set; }

        public string PackageDestinationFile { get; set; }

        public string PackageFilterFile { get; set; }

        public string PackageFormat { get; set; }

        public string PackagePlatform { get; set; }

        public string PackagePushApiKey { get; set; }

        public string PackagePushFile { get; set; }

        public string PackagePushUrl { get; set; }

        public string PackagePushVersion { get; set; }

        public string PackagePushPlatform { get; set; }

        public string PackagePushBranchToUpdate { get; set; }

        public bool PackagePushIgnoreOnExisting { get; set; }

        public string StartProjectTemplateURL { get; set; }

        public string StartProjectName { get; set; }

        public string ExecuteProjectName { get; set; }

        public string[] ExecuteProjectArguments { get; set; }

        public string ExecuteProjectConfiguration { get; set; }

        public string PackageRoot { get; set; }

        public bool DebugServiceResolution { get; set; }

        public bool DisablePackageResolution { get; set; }

        public string BuildModuleOrDefinitionName { get; set; }

        public string BuildTarget { get; set; }

        public Dictionary<string, string> BuildProperties { get; set; }

        public string AutomatedBuildScriptPath { get; set; }

        public bool DisableProjectGeneration { get; set; }

        public bool DisableHostProjectGeneration { get; set; }

        public string BuildProcessArchitecture { get; set; }

        public bool? UseTaskParallelisation { get; set; }

        public bool? SafePackageResolution { get; set; }

        public void SetCommandToExecuteIfNotDefault(ICommand command)
        {
            if (this.CommandToExecute is DefaultCommand)
            {
                this.CommandToExecute = command;
            }
            else
            {
                throw new InvalidOperationException(
                    "There is already a mode of operation set.");
            }
        }
    }
}

