using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Protobuild
{
    public class AddPackageCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 0 || args[0] == null)
            {
                throw new InvalidOperationException("You must provide an argument to the -add option");
            }

            pendingExecution.PackageUrl = args[0];
        }

        public int Execute(Execution execution)
        {
            var url = execution.PackageUrl;
            var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));

            if (module.Packages == null)
            {
                module.Packages = new List<PackageRef>();
            }

            var branch = "master";
            if (url.LastIndexOf('@') > url.LastIndexOf('/'))
            {
                // A branch / commit ref is specified.
                branch = url.Substring(url.LastIndexOf('@') + 1);
                url = url.Substring(0, url.LastIndexOf('@'));
            }

            var uri = new Uri(url);

            var package = new PackageRef
            {
                Uri = url,
                GitRef = branch,
                Folder = uri.AbsolutePath.Trim('/').Split('/').Last()
            };

            if (Directory.Exists(package.Folder))
            {
                throw new InvalidOperationException(package.Folder + " already exists");
            }

            if (module.Packages.Any(x => x.Uri == package.Uri))
            {
                Console.WriteLine("WARNING: Package with URI " + package.Uri + " is already present; ignoring request to add package.");
                return 0;
            }

            Console.WriteLine("Adding " + url + " as " + package.Folder + "...");
            module.Packages.Add(package);
            module.Save(Path.Combine("Build", "Module.xml"));

            return 0;
        }

        public string GetDescription()
        {
            return @"
Add a package to the current module.
";
        }

        public int GetArgCount()
        {
            return 1;
        }

        public string[] GetArgNames()
        {
            return new[] { "package_url" };
        }
    }
}

