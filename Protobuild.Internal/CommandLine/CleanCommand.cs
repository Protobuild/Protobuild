﻿using System;
using System.IO;

namespace Protobuild
{
    internal class CleanCommand : ICommand
    {
        private readonly IActionDispatch m_ActionDispatch;

        public CleanCommand(IActionDispatch actionDispatch)
        {
            this.m_ActionDispatch = actionDispatch;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length > 0)
            {
                pendingExecution.Platform = args[0];
            }
        }

        public int Execute(Execution execution)
        {
            if (Directory.Exists("Build"))
            {
                var module = ModuleInfo.Load(Path.Combine(execution.WorkingDirectory, "Build", "Module.xml"));
                return this.m_ActionDispatch.PerformAction(
                    execution.WorkingDirectory,
                    module,
                    "clean",
                    execution.Platform,
                    execution.EnabledServices.ToArray(),
                    execution.DisabledServices.ToArray(),
                    execution.ServiceSpecificationPath,
                    execution.DebugServiceResolution,
                    execution.DisablePackageResolution,
                    execution.DisableHostProjectGeneration,
                    execution.UseTaskParallelisation,
                    execution.SafePackageResolution)
                    ? 0
                    : 1;
            }

            return 1;
        }

        public string GetDescription()
        {
            return @"
Removes all generated C# project and solution files.  If no
platform is specified, cleans for the current platform.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "platform?" };
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

