using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Protobuild
{
    internal class RepushPackageCommand : ICommand
    {
        private readonly IPackageLookup _packageLookup;

        private readonly BinaryPackageResolve _binaryPackageResolve;

        private readonly IPackageUrlParser _packageUrlParser;

        private readonly IFeatureManager _featureManager;

        public RepushPackageCommand(
            IPackageLookup packageLookup,
            BinaryPackageResolve binaryPackageResolve,
            IPackageUrlParser packageUrlParser,
            IFeatureManager featureManager)
        {
            _packageLookup = packageLookup;
            _binaryPackageResolve = binaryPackageResolve;
            _packageUrlParser = packageUrlParser;
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length < 5 || args[0] == null || args[1] == null || args[2] == null || args[3] == null || args[4] == null)
            {
                throw new InvalidOperationException("You must provide all arguments to -push except for the branch name.");
            }

            if (File.Exists(args[0]))
            {
                using (var reader = new StreamReader(args[0]))
                {
                    pendingExecution.PackagePushApiKey = reader.ReadToEnd().Trim();
                }
            }
            else
            {
                pendingExecution.PackagePushApiKey = args[0];
            }

            pendingExecution.PackageUrl = args[1].TrimEnd('/');
            pendingExecution.PackagePushUrl = args[2].TrimEnd('/');
            pendingExecution.PackagePushVersion = args[3];
            pendingExecution.PackagePushPlatform = args[4];
            pendingExecution.PackagePushBranchToUpdate = args.Length >= 6 ? args[5] : null;
        }

        public int Execute(Execution execution)
        {
            using (var client = new RetryableWebClient())
            {
                var sourcePackage = _packageUrlParser.Parse(execution.PackageUrl);

                Console.WriteLine("Retrieving source package...");
                var metadata = _packageLookup.Lookup(new PackageRequestRef(
                    sourcePackage.Uri, 
                    sourcePackage.GitRef,
                    execution.PackagePushPlatform,
                    false));

                if (metadata.GetProtobuildPackageBinary == null)
                {
                    Console.Error.WriteLine(
                        "ERROR: URL resolved to a package metadata type '" + metadata.GetType().Name + "', " +
                        "but this type doesn't provide a mechanism to convert to a Protobuild package.");
                }

                string archiveType;
                byte[] archiveData;
                metadata.GetProtobuildPackageBinary(metadata, out archiveType, out archiveData);

                var protobuildPackageMetadata = metadata as ProtobuildPackageMetadata;
                if (protobuildPackageMetadata == null)
                {
                    Console.Error.WriteLine("--repush requires that the source URL resolve to a Protobuild package");
                    return 1;
                }
                
                Console.WriteLine("Detected package type as " + archiveType + ".");

                if (execution.PackagePushVersion.StartsWith("hash:", StringComparison.InvariantCulture))
                {
                    var sha1 = new SHA1Managed();
                    var hashed = sha1.ComputeHash(Encoding.ASCII.GetBytes(execution.PackagePushVersion.Substring("hash:".Length)));
                    execution.PackagePushVersion = BitConverter.ToString(hashed).ToLowerInvariant().Replace("-", "");
                }

                Console.WriteLine("Creating new package version...");

                var uploadParameters = new System.Collections.Specialized.NameValueCollection
                {
                    { "__apikey__", execution.PackagePushApiKey },
                    { "version", execution.PackagePushVersion },
                    { "platform", execution.PackagePushPlatform },
                };
                
                var json = fastJSON.JSON.ToDynamic(
                    System.Text.Encoding.ASCII.GetString(
                        client.UploadValues(execution.PackagePushUrl + "/version/new/api", uploadParameters)));

                if (json.has_error)
                {
                    Console.WriteLine(json.error);
                    return 1;
                }

                var uploadTarget = (string)json.result.uploadUrl;
                var finalizeTarget = (string)json.result.finalizeUrl;

                Console.WriteLine("Uploading package...");
                this.PushBinary(uploadTarget, archiveData);

                Console.WriteLine("Finalizing package version...");

                var finalizeParameters = new System.Collections.Specialized.NameValueCollection
                {
                    { "__apikey__", execution.PackagePushApiKey },
                    { "archiveType", archiveType },
                };

                json = fastJSON.JSON.ToDynamic(
                    System.Text.Encoding.ASCII.GetString(
                        client.UploadValues(finalizeTarget, finalizeParameters)));

                if (json.has_error)
                {
                    Console.WriteLine(json.error);
                    return 1;
                }

                if (execution.PackagePushBranchToUpdate != null)
                {
                    Console.WriteLine("Updating branch " + execution.PackagePushBranchToUpdate + " to point at new version...");

                    var branchUpdateParameters = new System.Collections.Specialized.NameValueCollection
                    {
                        { "__apikey__", execution.PackagePushApiKey },
                        { "name", execution.PackagePushBranchToUpdate },
                        { "git", execution.PackagePushVersion },
                    };

                    json = fastJSON.JSON.ToDynamic(
                        System.Text.Encoding.ASCII.GetString(
                            client.UploadValues(
                                execution.PackagePushUrl + "/branch/edit/" + execution.PackagePushBranchToUpdate + "/api", 
                                branchUpdateParameters)));

                    if (json.has_error)
                    {
                        Console.WriteLine(json.error);
                        return 1;
                    }
                }

                Console.WriteLine("Package version repushed successfully.");
            }
            return 0;
        }

        public string GetDescription()
        {
            return @"
Downloads a package from a specified URL and pushes it back to
another repository URL.  This can be used to mirror repositories and
import NuGet packages.
";
        }

        public int GetArgCount()
        {
            return 6;
        }

        public string[] GetArgNames()
        {
            return new[] { "api_key_or_key_file", "src_url", "dest_url", "version", "platform", "branch_to_update?" };
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

        private void PushBinary(string targetUri, byte[] bytes)
        {
            using (var client = new RetryableWebClient())
            {
                var done = false;
                byte[] result = null;
                Exception ex = null;
                var uploadProgressRenderer = new UploadProgressRenderer();
                client.UploadDataCompleted += (sender, e) => {
                    if (e.Error != null)
                    {
                        ex = e.Error;
                    }

                    result = e.Result;
                    done = true;
                };
                client.UploadProgressChanged += (sender, e) => {
                    if (!done)
                    {
                        uploadProgressRenderer.Update(e.ProgressPercentage, e.BytesSent / 1024);
                    }
                };
                client.UploadDataAsync(new Uri(targetUri), "PUT", bytes);
                while (!done)
                {
                    System.Threading.Thread.Sleep(0);
                }

                uploadProgressRenderer.FinalizeRendering();

                if (ex != null)
                {
                    throw new InvalidOperationException("Upload error", ex);
                }

                return;
            }
        }
    }
}

