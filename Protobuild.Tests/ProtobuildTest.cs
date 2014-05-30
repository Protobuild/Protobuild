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
            var moduleInfoType = typeof(ModuleInfo);

            this.m_TestName = name;

            var location = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            var dataLocation = Path.Combine(location, @"..\..\TestData", this.m_TestName);

            var protobuildLocation =
                AppDomain.CurrentDomain.GetAssemblies().First(x => x.GetName().Name == "Protobuild").Location;

            this.m_TestLocation = dataLocation;

            this.DeployProtobuildToTestFolder(dataLocation, protobuildLocation);
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

        protected void Generate(string platform = null, string args = null)
        {
            var pi = new ProcessStartInfo
            {
                FileName = Path.Combine(this.m_TestLocation, "Protobuild.exe"),
                Arguments = "--generate " + (platform ?? "Windows") + " " + (args ?? string.Empty),
                WorkingDirectory = this.m_TestLocation,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var p = new Process { StartInfo = pi };
            p.Start();
            p.WaitForExit();
        }

        protected string ReadFile(string path)
        {
            using (var reader = new StreamReader(Path.Combine(this.m_TestLocation, path)))
            {
                return reader.ReadToEnd();
            }
        }

        protected string GetPath(string path)
        {
            return Path.Combine(this.m_TestLocation, path);
        }
    }
}