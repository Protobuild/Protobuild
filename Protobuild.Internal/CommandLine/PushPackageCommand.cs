using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Protobuild
{
    public class PushPackageCommand : ICommand
    {
        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length < 5 || args[0] == null || args[1] == null || args[2] == null || args[3] == null || args[4] == null)
            {
                throw new InvalidOperationException("You must provide all arguments to -push except for the branch name.");
            }

            pendingExecution.PackagePushApiKey = args[0];
            pendingExecution.PackagePushFile = new FileInfo(args[1]).FullName;
            pendingExecution.PackagePushUrl = args[2].TrimEnd('/');
            pendingExecution.PackagePushVersion = args[3];
            pendingExecution.PackagePushPlatform = args[4];
            pendingExecution.PackagePushBranchToUpdate = args.Length >= 6 ? args[5] : null;
        }

        public int Execute(Execution execution)
        {
            using (var client = new WebClient())
            {
                var archiveType = this.DetectPackageType(execution.PackagePushFile);

                Console.WriteLine("Detected package type as " + archiveType + ".");

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

                Console.Write("Uploading package...");
                this.PushBinary(uploadTarget, execution.PackagePushFile);

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

                Console.WriteLine("Package version pushed successfully.");
            }
            return 0;
        }

        public string GetDescription()
        {
            return @"
Pushes the specified file to the package repository URL, using
the given API key.  ""version"" should be the SHA1 Git hash that the
package was built from, and ""platform"" should be the platform that
the package was built for.  If ""branch_to_update"" is specified, then
the given branch will be created or updated to point to the new
version being uploaded.  When pushing to the official repository, the
package URL should look like ""http://protobuild.org/MyAccount/MyPackage"".
";
        }

        public int GetArgCount()
        {
            return 6;
        }

        public string[] GetArgNames()
        {
            return new[] { "api_key", "file", "url", "version", "platform", "branch_to_update?" };
        }

        private class AccurateWebClient : WebClient
        {
            private readonly int m_ContentLength;

            public AccurateWebClient(int length)
            {
                this.m_ContentLength = length;
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var req = base.GetWebRequest(address) as HttpWebRequest;
                req.AllowWriteStreamBuffering = false;
                req.ContentLength = this.m_ContentLength;
                return req;
            }
        }

        private void PushBinary(string targetUri, string file)
        {
            byte[] bytes;
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                bytes = new byte[(int)stream.Length];
                stream.Read(bytes, 0, bytes.Length);
            }

            try 
            {
                using (var client = new AccurateWebClient(bytes.Length))
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

                    Console.WriteLine();

                    if (ex != null)
                    {
                        throw new InvalidOperationException("Upload error", ex);
                    }

                    return;
                }
            }
            catch (WebException)
            {
                Console.WriteLine("Web exception when sending to: " + targetUri);
                throw;
            }
        }

        private string DetectPackageType(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                try
                {
                    using (var gzip = new GZipStream(stream, CompressionMode.Decompress, true))
                    {
                        using (var reader = new StreamReader(gzip))
                        {
                            reader.ReadToEnd();
                        }
                    }

                    return PackageManager.ARCHIVE_FORMAT_TAR_GZIP;
                }
                catch
                {
                    stream.Seek(0, SeekOrigin.Begin);

                    try
                    {
                        using (var memory = new MemoryStream())
                        {
                            LZMA.LzmaHelper.Decompress(stream, memory);
                        }

                        return PackageManager.ARCHIVE_FORMAT_TAR_LZMA;
                    }
                    catch
                    {
                        throw new InvalidOperationException("Package format not recognised for " + file);
                    }
                }
            }
        }
    }
}

