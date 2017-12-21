using System;
using System.IO;

namespace Protobuild
{
    internal class PackageGitCommitCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public PackageGitCommitCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the repository URL to -package-git-commit.");
            }

            pendingExecution.PackageGitCommit = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetShortCategory()
        {
            return "Package management";
        }

        public string GetShortDescription()
        {
            return "sets the Git commit hash associated with the package";
        }

        public string GetDescription()
        {
            return @"
Sets the Git commit associated with the package.  If this option
is not used, the commit information will be omitted from the package.
This option has no effect for packages not stored in the nuget/zip format.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetShortArgNames()
        {
            return new[] { "commit_hash" };
        }

        public string[] GetArgNames()
        {
            return new[] { "package-git-commit" };
        }

        public bool IsInternal()
        {
            return false;
        }

        public bool IsRecognised()
        {
            return _featureManager.IsFeatureEnabled(Feature.PackageManagement);
        }

        public bool IsIgnored()
        {
            return false;
        }
    }
}

