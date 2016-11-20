using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Protobuild.Internal;

namespace Protobuild
{
    internal class PushPackageCommand : ICommand
    {
        private readonly IFeatureManager _featureManager;

        const string ArgumentOmitted = "\0argument-omitted";

        public PushPackageCommand(IFeatureManager featureManager)
        {
            _featureManager = featureManager;
        }

        public void Encounter(Execution pendingExecution, string[] args)
        {
            pendingExecution.SetCommandToExecuteIfNotDefault(this);

            if (args.Length == 3)
            {
                // This is the new format for nuget/zip packages.
            }
            else if (args.Length < 5 || args[0] == null || args[1] == null || args[2] == null || args[3] == null || args[4] == null)
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

            pendingExecution.PackagePushFile = new FileInfo(args[1]).FullName;
            pendingExecution.PackagePushUrl = args[2].TrimEnd('/');
            pendingExecution.PackagePushVersion = args.Length == 3 ? ArgumentOmitted : args[3];
            pendingExecution.PackagePushPlatform = args.Length == 3 ? ArgumentOmitted : args[4];
            pendingExecution.PackagePushBranchToUpdate = args.Length >= 6 ? args[5] : null;
        }

        public int Execute(Execution execution)
        {
            var archiveType = this.DetectPackageType(execution.PackagePushFile);

            if (archiveType == PackageManager.ARCHIVE_FORMAT_NUGET_ZIP)
            {
                if (execution.PackagePushVersion != ArgumentOmitted || execution.PackagePushPlatform != ArgumentOmitted)
                {
                    Console.Error.WriteLine("You must omit the version and platform arguments when pushing packages in the NuGet format.");
                }
            }
            else
            {
                if (execution.PackagePushVersion == ArgumentOmitted || execution.PackagePushPlatform == ArgumentOmitted)
                {
                    Console.Error.WriteLine("You must provide the version and platform arguments.");
                }
            }

            Console.WriteLine("Detected package type as " + archiveType + ".");

            switch (archiveType)
            {
                case PackageManager.ARCHIVE_FORMAT_NUGET_ZIP:
                    return PushToNuGetRepository(execution);
                case PackageManager.ARCHIVE_FORMAT_TAR_GZIP:
                case PackageManager.ARCHIVE_FORMAT_TAR_LZMA:
                default:
                    return PushToProtobuildRepository(execution, archiveType);
            }
        }

        private int PushToNuGetRepository(Execution execution)
        {
            var isUnified = false;
            string platform = null;
            string commitHash = null;

            // Detect if it is a unified package, and if not the platform and Git hash that this package is
            // for by reading the package file.
            using (var storer = ZipStorer.Open(execution.PackagePushFile, FileAccess.Read))
            {
                var entries = storer.ReadCentralDir();
                
                var protobuildMetadata =
                    entries.Where(
                        x => x.FilenameInZip == "Package.xml").ToArray();

                if (protobuildMetadata.Length > 0)
                {
                    using (var memory = new MemoryStream())
                    {
                        storer.ExtractFile(protobuildMetadata[0], memory);
                        memory.Seek(0, SeekOrigin.Begin);
                        var document = new XmlDocument();
                        document.Load(memory);
                        var platforms = document.SelectNodes("/Package/BinaryPlatforms/Platform");
                        if (platforms != null)
                        {
                            if (platforms.Count == 1)
                            {
                                isUnified = false;
                                platform = platforms[0].InnerText;
                            }
                            else if (platforms.Count > 1)
                            {
                                isUnified = true;
                                platform = null;
                            }
                        }

                        var commitHashNode = document.SelectSingleNode("/Package/Source/GitCommitHash");
                        if (commitHashNode != null)
                        {
                            commitHash = commitHashNode.InnerText;
                        }
                    }
                }
            }

            // Only push semantic versions for packages which contain multiple platforms.
            if (isUnified)
            {
                var ret = PushToNuGetRepository(execution, false, platform, commitHash);
                if (ret != 0)
                {
                    return ret;
                }
            }

            return PushToNuGetRepository(execution, true, platform, commitHash);
        }

        private int PushToNuGetRepository(Execution execution, bool pushGitVersion, string platform, string commitHash)
        {
            if (pushGitVersion)
            {
                // We have to patch the package file in memory to include the Git hash
                // as part of the version.  This is required so that Protobuild
                // can resolve source-equivalent binary packages.
                Console.WriteLine("Patching package file to include Git version information...");

                using (var patchedPackage = new MemoryStream())
                {
                    using (var patchedPackageWriter = ZipStorer.Create(patchedPackage, string.Empty, true))
                    {
                        using (var packageReader = ZipStorer.Open(execution.PackagePushFile, FileAccess.Read))
                        {
                            var entries = packageReader.ReadCentralDir();

                            var progressRenderer = new PackagePatchProgressRenderer(entries.Count);
                            var i = 0;

                            foreach (var entry in packageReader.ReadCentralDir())
                            {
                                if (entry.FilenameInZip.EndsWith(".nuspec") && !entry.FilenameInZip.Contains("/"))
                                {
                                    // This is the NuGet specification file in the root of the package
                                    // that we need to patch.
                                    using (var fileStream = new MemoryStream())
                                    {
                                        packageReader.ExtractFile(entry, fileStream);
                                        fileStream.Seek(0, SeekOrigin.Begin);
                                        string nuspecContent;
                                        using (
                                            var reader = new StreamReader(fileStream, Encoding.UTF8, true, 4096, true))
                                        {
                                            nuspecContent = reader.ReadToEnd();

                                            var regex =
                                                new Regex("version\\>[0-9]+\\.[0-9]+\\.[0-9]+\\+([^\\<]*)\\<\\/version");

                                            nuspecContent = regex.Replace(nuspecContent,
                                                "version>" + NuGetVersionHelper.CreateNuGetPackageVersion(commitHash, platform) + "</version");

                                            using (var patchedFileStream = new MemoryStream())
                                            {
                                                using (
                                                    var writer = new StreamWriter(patchedFileStream, Encoding.UTF8, 4096,
                                                        true))
                                                {
                                                    writer.Write(nuspecContent);
                                                    writer.Flush();
                                                    patchedFileStream.Seek(0, SeekOrigin.Begin);
                                                    patchedPackageWriter.AddStream(
                                                        entry.Method,
                                                        entry.FilenameInZip,
                                                        patchedFileStream,
                                                        entry.ModifyTime,
                                                        entry.Comment,
                                                        true);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    using (var fileStream = new MemoryStream())
                                    {
                                        packageReader.ExtractFile(entry, fileStream);
                                        fileStream.Seek(0, SeekOrigin.Begin);
                                        patchedPackageWriter.AddStream(
                                            entry.Method,
                                            entry.FilenameInZip,
                                            fileStream,
                                            entry.ModifyTime,
                                            entry.Comment,
                                            true);
                                    }
                                }

                                i++;
                                progressRenderer.SetProgress(i);
                            }

                            progressRenderer.FinalizeRendering();
                        }
                    }

                    patchedPackage.Seek(0, SeekOrigin.Begin);

                    // Push the patched package to the NuGet repository.
                    Console.WriteLine("Uploading package with Git version...");
                    return this.PushNuGetBinary(execution.PackagePushUrl, execution.PackagePushApiKey,
                        patchedPackage, execution.PackagePushIgnoreOnExisting);
                }
            }
            else
            {
                Console.WriteLine("Uploading package with semantic version...");
                using (
                    var stream = new FileStream(execution.PackagePushFile, FileMode.Open, FileAccess.Read,
                        FileShare.Read))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        // Note about the true argument here: We always ignore a conflict for the semantic version, as
                        // the build server may be pushing on every commit.  In this scenario, developers will leave
                        // the semantic version as-is until they're ready to release a new semantic version.
                        return this.PushNuGetBinary(execution.PackagePushUrl, execution.PackagePushApiKey,
                            memoryStream, true);
                    }
                }
            }
        }

        private int PushToProtobuildRepository(Execution execution, string archiveType)
        {
            using (var client = new RetryableWebClient())
            {
                Console.WriteLine("Creating new package version...");

                if (execution.PackagePushVersion.StartsWith("hash:", StringComparison.InvariantCulture))
                {
                    var sha1 = new SHA1Managed();
                    var hashed = sha1.ComputeHash(Encoding.ASCII.GetBytes(execution.PackagePushVersion.Substring("hash:".Length)));
                    execution.PackagePushVersion = BitConverter.ToString(hashed).ToLowerInvariant().Replace("-", "");
                }

                var uploadParameters = new System.Collections.Specialized.NameValueCollection
                {
                    { "__apikey__", execution.PackagePushApiKey },
                    { "version", execution.PackagePushVersion },
                    { "platform", execution.PackagePushPlatform },
                };

                byte[] versionData;
                try
                {
                    versionData = client.UploadValues(execution.PackagePushUrl + "/version/new/api", uploadParameters);
                }
                catch (WebException ex)
                {
                    var responseData = string.Empty;

                    // Try and get the full response from the server to display in the exception message.
                    try
                    {
                        var stream = ex.Response.GetResponseStream();
                        if (stream != null)
                        {
                            using (var reader = new StreamReader(stream, Encoding.Default, true, 4096, true))
                            {
                                responseData = reader.ReadToEnd();
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }

                    throw new WebException(ex.Message + "  Content of response was: " + responseData);
                }

                var json = fastJSON.JSON.ToDynamic(
                    System.Text.Encoding.ASCII.GetString(
                        versionData));

                if (json.has_error)
                {
                    Console.WriteLine(json.error);

                    if (execution.PackagePushIgnoreOnExisting &&
                        ((string)json.error.ToString()).Contains("Another version already exists with this Git hash and platform"))
                    {
                        return 0;
                    }

                    return 1;
                }

                var uploadTarget = (string)json.result.uploadUrl;
                var finalizeTarget = (string)json.result.finalizeUrl;

                Console.WriteLine("Uploading package...");
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
the given API key.  If the API key is the name of a file on disk, that
file will be read with the expectation it contains the API key.  Otherwise
the API key will be used as-is.  ""version"" should be the SHA1 Git hash that the
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
            return new[] { "api_key_or_key_file", "file", "url", "version?", "platform?", "branch_to_update?" };
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

        private void PushBinary(string targetUri, string file)
        {
            byte[] bytes;
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                bytes = new byte[(int)stream.Length];
                stream.Read(bytes, 0, bytes.Length);
            }

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

        private int PushNuGetBinary(string targetUri, string apiKey, MemoryStream file, bool ignoreOnExisting)
        {
            var task = Task.Run(async () => await PushNuGetBinaryAsync(targetUri, apiKey, file, ignoreOnExisting));
            task.Wait();
            return task.Result;
        }

        private async Task<int> PushNuGetBinaryAsync(string targetUri, string apiKey, MemoryStream file, bool ignoreOnExisting)
        {
            byte[] fileBytes;
            fileBytes = new byte[(int)file.Length];
            file.Read(fileBytes, 0, fileBytes.Length);

            const string ApiKeyHeader = "X-NuGet-ApiKey";

            var boundary = Guid.NewGuid().ToString();

            int requestLength;
            byte[] combinedContent;
            using (var requestContent = new MemoryStream())
            {
                byte[] boundaryBytes = Encoding.UTF8.GetBytes("--" + boundary + "\r\n");
                byte[] trailer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                string fileheaderTemplate =
                    "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\";\r\nContent-Type: {2}\r\n\r\n";

                Action<Stream, string> writeString = (_stream, _txt) =>
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(_txt);
                    _stream.Write(bytes, 0, bytes.Length);
                };

                requestContent.Write(boundaryBytes, 0, boundaryBytes.Length);
                writeString(requestContent,
                    string.Format(fileheaderTemplate, "package", "package.nupkg", "application/octet-stream"));
                requestContent.Write(fileBytes, 0, fileBytes.Length);
                requestContent.Write(trailer, 0, trailer.Length);

                requestLength = (int)requestContent.Position;
                requestContent.Seek(0, SeekOrigin.Begin);
                combinedContent = new byte[requestLength];
                await requestContent.ReadAsync(combinedContent, 0, requestLength);
            }
                
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

                    try
                    {
                        result = e.Result;
                    }
                    catch { }
                    done = true;
                };
                client.UploadProgressChanged += (sender, e) => {
                    if (!done)
                    {
                        uploadProgressRenderer.Update(e.ProgressPercentage, e.BytesSent / 1024);
                    }
                };

                client.SetHeader("Content-Type", "multipart/form-data; boundary=\"" + boundary + "\"");
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.SetHeader(ApiKeyHeader, apiKey);
                }

                client.UploadDataAsync(new Uri(targetUri), "PUT", combinedContent);
                while (!done)
                {
                    System.Threading.Thread.Sleep(0);
                }

                uploadProgressRenderer.FinalizeRendering();

                if (ex != null)
                {
                    var webException = ex as WebException;
                    if (webException != null)
                    {
                        var httpResponse = webException.Response as HttpWebResponse;
                        if (httpResponse != null)
                        {
                            Console.Error.WriteLine(httpResponse.StatusDescription);

                            if (httpResponse.StatusCode == HttpStatusCode.Conflict)
                            {
                                if (ignoreOnExisting)
                                {
                                    // This is okay - the user doesn't care if there's an existing package with the same version.
                                    return 0;
                                }
                                else
                                {
                                    return 1;
                                }
                            }

                            return 1;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Upload error", ex);
                    }
                }

                if (result != null)
                {
                    var resultDecoded = Encoding.UTF8.GetString(result);
                    if (!string.IsNullOrWhiteSpace(resultDecoded))
                    {
                        Console.WriteLine(Encoding.UTF8.GetString(result));
                    }
                    else
                    {
                        Console.WriteLine("Package uploaded successfully");
                    }
                }
                else
                {
                    Console.WriteLine("Package uploaded successfully");
                }

                return 0;
            }
        }

        private string DetectPackageType(string file)
        {
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                try
                {
                    using (var zip = ZipStorer.Open(stream, FileAccess.Read, false))
                    {
                        zip.ReadCentralDir();

                        return PackageManager.ARCHIVE_FORMAT_NUGET_ZIP;
                    }
                }
                catch (ExecEnvironment.SelfInvokeExitException)
                {
                    throw;
                }
                catch
                {
                    stream.Seek(0, SeekOrigin.Begin);

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
                    catch (ExecEnvironment.SelfInvokeExitException)
                    {
                        throw;
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
                        catch (ExecEnvironment.SelfInvokeExitException)
                        {
                            throw;
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
}

