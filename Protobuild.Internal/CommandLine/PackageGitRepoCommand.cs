using System;
using System.IO;

namespace Protobuild
{
    internal class PackageGitRepoCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        public PackageGitRepoCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            if (args.Length < 1 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide the repository URL to -package-git-repo.");
            }

            pendingExecution.PackageGitRepositoryUrl = args[0];
        }

        public int Execute(Execution execution)
        {
            throw new NotSupportedException();
        }

        public string GetDescription()
        {
            return @"
Sets the Git repository URL associated with the package.  If this option
is not used, the repository URL information will be omitted.  This option has no effect
for packages not stored in the nuget/zip format.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "package-git-repo" };
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

