using System;
using System.Collections.Generic;

namespace Protobuild
{
    public class Execution
    {
        public Execution()
        {
            this.EnabledServices = new List<string>();
            this.DisabledServices = new List<string>();
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

