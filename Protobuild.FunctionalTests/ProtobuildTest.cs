using System.IO.Compression;
using Prototest.Library.Version1;

namespace Protobuild.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public abstract class ProtobuildTest
    {
        private readonly IAssert _assert;

        private string m_TestName;

        private string m_TestLocation;

        private string _protobuildName;

        protected ProtobuildTest(IAssert assert)
        {
            _assert = assert;
        }

        protected void SetupTest(string name, bool isPackTest = false, string parent = null, string child = null)
        {
            // This is used to ensure Protobuild.exe is referenced.
            _protobuildName = typeof(Protobuild.Bootstrap.Program).FullName;

            this.m_TestName = name;

            var location = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            var dataLocation = Path.Combine(location, "..", "..", "..", "..", "TestData", this.m_TestName);

            var protobuildLocation =
                AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == "Protobuild").Location;

            this.m_TestLocation = dataLocation;

            this.DeployProtobuildToTestFolder(dataLocation, protobuildLocation, isPackTest, parent, child);
        }

        private void PurgeSolutionsAndProjects(string dataLocation)
        {
            var dir = new DirectoryInfo(dataLocation);

            foreach (var solution in dir.GetFiles("*.sln"))
            {
                solution.Delete();
            }

            foreach (var project in dir.GetFiles("*.csproj"))
            {
                project.Delete();
            }

            foreach (var sub in dir.GetDirectories())
            {
                this.PurgeSolutionsAndProjects(sub.FullName);
            }
        }

        private void DeployProtobuildToTestFolder(string dataLocation, string protobuildLocation, bool isPackTest, string parent, string child)
        {
            if (parent == null)
            {
                File.Copy(protobuildLocation, Path.Combine(dataLocation, "Protobuild.exe"), true);
            }
            else
            {
                File.Copy(parent, Path.Combine(dataLocation, "Protobuild.exe"), true);
            }

            if (!isPackTest)
            {
                foreach (var dir in new DirectoryInfo(dataLocation).GetDirectories())
                {
                    if (dir.GetDirectories().Any(x => x.Name == "Build"))
                    {
                        this.DeployProtobuildToTestFolder(dir.FullName, protobuildLocation, false, child, child);
                    }
                }
            }
        }

        protected Tuple<string, string> Generate(string platform = null, string args = null, bool expectFailure = false, bool capture = false, string hostPlatform = null)
        {
            return this.OtherMode("generate", (platform ?? "Windows") + " " + args, expectFailure, capture: capture, hostPlatform: hostPlatform);
        }

        protected Tuple<string, string> OtherMode(string mode, string args = null, bool expectFailure = false, bool purge = true, bool capture = false, string workingSubdirectory = null, string hostPlatform = null)
        {
            if (purge)
            {
                this.PurgeSolutionsAndProjects(this.m_TestLocation);
            }

            var stdout = string.Empty;
            var stderr = string.Empty;

            var simulate = string.Empty;
            if (hostPlatform != null)
            {
                simulate = "--simulate-host-platform " + hostPlatform + " ";
            }

            var pi = new ProcessStartInfo
            {
                FileName = Path.Combine(this.m_TestLocation, "Protobuild.exe"),
                Arguments = simulate + "--" + mode + " " + (args ?? string.Empty),
                WorkingDirectory = workingSubdirectory != null ? Path.Combine(this.m_TestLocation, workingSubdirectory) : this.m_TestLocation,
                CreateNoWindow = capture,
                UseShellExecute = false,
                RedirectStandardError = capture,
                RedirectStandardOutput = capture,
            };
            var p = new Process { StartInfo = pi };
            if (capture)
            {
                p.OutputDataReceived += (sender, eventArgs) =>
                {
                    if (!string.IsNullOrEmpty(eventArgs.Data))
                    {
                        stdout += eventArgs.Data + "\n";
                    }
                };
                p.ErrorDataReceived += (sender, eventArgs) =>
                {
                    if (!string.IsNullOrEmpty(eventArgs.Data))
                    {
                        stderr += eventArgs.Data + "\n";
                    }
                };
            }
            p.Start();
            if (capture)
            {
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }
            p.WaitForExit();

            if (p.ExitCode == 134)
            {
                // SIGSEGV due to Mono bugs, try again.
                return this.OtherMode(mode, args, expectFailure, purge, capture);
            }

            if (expectFailure)
            {
                _assert.True(1 == p.ExitCode, "Expected command '" + pi.FileName + " " + pi.Arguments + "' to fail, but got successful exit code.");
            }
            else
            {
                _assert.True(0 == p.ExitCode, "Expected command '" + pi.FileName + " " + pi.Arguments + "' to succeed, but got failure exit code.");
            }

            return new Tuple<string, string>(stdout, stderr);
        }

        protected string ReadFile(string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);

            using (var reader = new StreamReader(Path.Combine(this.m_TestLocation, path)))
            {
                return reader.ReadToEnd();
            }
        }

        protected string GetPath(string path)
        {
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            return Path.Combine(this.m_TestLocation, path);
        }

        protected Dictionary<string, byte[]> LoadPackage(string path)
        {
            if (path.EndsWith(".tar.lzma"))
            {
                using (var lzma = new FileStream(Path.Combine(m_TestLocation, path), FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (var decompress = new MemoryStream())
                    {
                        LZMA.LzmaHelper.Decompress(lzma, decompress);
                        decompress.Seek(0, SeekOrigin.Begin);

                        var archive = new tar_cs.TarReader(decompress);
                        var deduplicator = new Reduplicator();
                        return deduplicator.UnpackTarToMemory(archive);
                    }
                }
            }
            else if (path.EndsWith(".tar.gz"))
            {
                using (var file = new FileStream(Path.Combine(m_TestLocation, path), FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (var gzip = new GZipStream(file, CompressionMode.Decompress))
                    {
                        var archive = new tar_cs.TarReader(gzip);
                        var deduplicator = new Reduplicator();
                        return deduplicator.UnpackTarToMemory(archive);
                    }
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        protected string SetupSrcPackage()
        {
            var protobuildLocation =
                AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == "Protobuild").Location;

            var location = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            var dataLocation = Path.Combine(location, "..", "..", "..", "..", "TestData", "SrcPackage");

            var tempLocation = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            CopyDirectory(dataLocation, tempLocation);

            File.Copy(protobuildLocation, Path.Combine(tempLocation, "Protobuild.exe"), true);

            RunGitAndCapture(tempLocation, "init");
            RunGitAndCapture(tempLocation, "config user.email temp@temp.com");
            RunGitAndCapture(tempLocation, "config user.name Temp");
            RunGitAndCapture(tempLocation, "add -f .");
            RunGitAndCapture(tempLocation, "commit -a -m 'temp'");

            return tempLocation;
        }

        protected string SetupSrcTemplate()
        {
            var location = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            var dataLocation = Path.Combine(location, "..", "..", "..", "..", "TestData", "SrcTemplate");

            var tempLocation = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            CopyDirectory(dataLocation, tempLocation);

            RunGitAndCapture(tempLocation, "init");
            RunGitAndCapture(tempLocation, "config user.email temp@temp.com");
            RunGitAndCapture(tempLocation, "config user.name Temp");
            RunGitAndCapture(tempLocation, "add -f .");
            RunGitAndCapture(tempLocation, "commit -a -m 'temp'");

            return tempLocation;
        }

        protected string SetupSrcTemplateAlt()
        {
            var location = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            var dataLocation = Path.Combine(location, "..", "..", "..", "..", "TestData", "SrcTemplateAlt");

            var tempLocation = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            CopyDirectory(dataLocation, tempLocation);

            RunGitAndCapture(tempLocation, "init");
            RunGitAndCapture(tempLocation, "config user.email temp@temp.com");
            RunGitAndCapture(tempLocation, "config user.name Temp");
            RunGitAndCapture(tempLocation, "add -f .");
            RunGitAndCapture(tempLocation, "commit -a -m 'temp'");

            return tempLocation;
        }

        private static void CopyDirectory(string source, string dest)
        {
            var dir = new DirectoryInfo(source);

            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            foreach (var file in dir.GetFiles())
            {
                var temppath = Path.Combine(dest, file.Name);
                file.CopyTo(temppath, true);
            }

            foreach (var subdir in dir.GetDirectories())
            {
                var temppath = Path.Combine(dest, subdir.Name);
                CopyDirectory(subdir.FullName, temppath);
            }
        }

        private static string RunGitAndCapture(string folder, string str)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = str,
                WorkingDirectory = folder,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            var process = Process.Start(processStartInfo);
            var result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Got an unexpected exit code of " + process.ExitCode + " from Git");
            }

            return result;
        }
    }
}
