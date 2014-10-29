namespace Protobuild.Tests
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public abstract class ProtobuildTest
    {
        private string m_TestName;

        private string m_TestLocation;

        protected void SetupTest(string name)
        {
            // This is used to ensure Protobuild.exe is referenced.
            Console.WriteLine(typeof(Protobuild.Bootstrap.Program).FullName);

            this.m_TestName = name;

            var location = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            var dataLocation = Path.Combine(location, "..", "..", "..", "..", "TestData", this.m_TestName);

            var protobuildLocation =
                AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == "Protobuild").Location;

            this.m_TestLocation = dataLocation;

            this.DeployProtobuildToTestFolder(dataLocation, protobuildLocation);
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

        private void DeployProtobuildToTestFolder(string dataLocation, string protobuildLocation)
        {
            File.Copy(protobuildLocation, Path.Combine(dataLocation, "Protobuild.exe"), true);

            foreach (var dir in new DirectoryInfo(dataLocation).GetDirectories())
            {
                if (dir.GetDirectories().Any(x => x.Name == "Build"))
                {
                    this.DeployProtobuildToTestFolder(dir.FullName, protobuildLocation);
                }
            }
        }

        protected void Generate(string platform = null, string args = null, bool expectFailure = false)
        {
            this.OtherMode("generate", platform, args, expectFailure);
        }

        protected void OtherMode(string mode, string platform = null, string args = null, bool expectFailure = false, bool purge = true)
        {
            if (purge)
            {
                this.PurgeSolutionsAndProjects(this.m_TestLocation);
            }

            var pi = new ProcessStartInfo
            {
                FileName = Path.Combine(this.m_TestLocation, "Protobuild.exe"),
                Arguments = "--" + mode + " " + (platform ?? "Windows") + " " + (args ?? string.Empty),
                WorkingDirectory = this.m_TestLocation,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            var p = new Process { StartInfo = pi };
            p.OutputDataReceived += (sender, eventArgs) =>
            {
                if (!string.IsNullOrEmpty(eventArgs.Data))
                {
                    Console.WriteLine(eventArgs.Data);
                }
            };
            p.ErrorDataReceived += (sender, eventArgs) =>
            {
                if (!string.IsNullOrEmpty(eventArgs.Data))
                {
                    Console.WriteLine(eventArgs.Data);
                }
            };
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            if (expectFailure)
            {
                Xunit.Assert.Equal(1, p.ExitCode);
            }
            else
            {
                Xunit.Assert.Equal(0, p.ExitCode);
            }
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
    }
}
