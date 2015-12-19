using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Protobuild.RegressionTestTool
{
    public static class Program
    {
        private static string ModuleRoot;

        private class CommitInfo
        {
            public string ExecutableName;

            public int CommitTimestamp;

            public string CommitHash;

            public string CommitMessage;
        }

        private static string FindBash()
        {
            var bash = new string[]
            {
                @"C:\Program Files (x86)\Git\bin\bash.exe",
                @"C:\Program Files\Git\bin\bash.exe",
            };

            var found = bash.FirstOrDefault(File.Exists);
            if (found == null)
            {
                throw new Exception("Unable to find Bash for regression test preparation.");
            }

            return found;
        }

        private static string FindGit()
        {
            var git = new string[]
            {
                @"C:\Program Files (x86)\Git\cmd\git.exe",
                @"C:\Program Files\Git\cmd\git.exe",
            };

            var found = git.FirstOrDefault(File.Exists);
            if (found == null)
            {
                throw new Exception("Unable to find Git for regression test preparation.");
            }

            return found;
        }

        public static void Main(string[] args)
        {
            ModuleRoot = new FileInfo(typeof(Program).Assembly.Location).Directory?.Parent?.Parent?.Parent?.Parent?.Parent?.FullName;

            if (ModuleRoot == null)
            {
                throw new InvalidOperationException("Unable to determine module root");
            }

            if (args[0] == "prepare")
            {
                var commits = GetAllCommitsWithProtobuild().ToArray();

                Directory.CreateDirectory(Path.Combine(ModuleRoot, "Protobuild.FunctionalTests", "PreviousVersions"));

                var completed = 0;
                var completedLock = new object();
                var tasks = from commit in commits
                    select Task.Run(() =>
                    {
                        if (commit.CommitHash.StartsWith("7f0242680") ||
                            commit.CommitHash.StartsWith("7e7ddb77a") ||
                            commit.CommitHash.StartsWith("cf231b244") ||
                            commit.CommitHash.StartsWith("f90fdcf2d"))
                        {
                            // These are known bad commits, exclude them.
                            return;
                        }

                        if (!File.Exists(Path.Combine(ModuleRoot, "Protobuild.FunctionalTests", "PreviousVersions",
                            commit.ExecutableName)))
                        {
                            var process = new Process();
                            process.StartInfo = new ProcessStartInfo
                            {
                                FileName = FindBash(),
                                Arguments =
                                    @"-c ""git cat-file blob " + commit.CommitHash +
                                    @":Protobuild.exe > Protobuild.FunctionalTests/PreviousVersions/" +
                                    commit.ExecutableName + "\"",
                                WorkingDirectory = ModuleRoot,
                                UseShellExecute = false,
                            };
                            process.Start();
                            process.WaitForExit();
                        }

                        using (
                            var writer =
                                new StreamWriter(Path.Combine(ModuleRoot, "Protobuild.FunctionalTests", "PreviousVersions",
                                    commit.CommitHash + ".txt")))
                        {
                            writer.Write(commit.CommitMessage);
                        }

                        lock (completedLock)
                        {
                            completed++;
                            Console.WriteLine("Extracted Protobuild from commit hash (" + completed + "/" + commits.Length + ") " + commit.CommitHash);
                        }
                    });

                Task.WaitAll(tasks.ToArray());
            }
        }

        private static IEnumerable<CommitInfo> GetAllCommitsWithProtobuild()
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = FindGit(),
                Arguments = @"log --pretty=""Protobuild-%ct-%H.exe %ct %H %s"" -- Protobuild.exe",
                WorkingDirectory = ModuleRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            process.Start();
            var content = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return from line in content.Split('\n')
                select line.Trim()
                into lineTrimmed
                where !string.IsNullOrWhiteSpace(lineTrimmed)
                select lineTrimmed.Split(new[] {' '}, 4)
                into components
                let timestamp = int.Parse(components[1])
                where timestamp > 1411400075
                select new CommitInfo
                {
                    ExecutableName = components[0],
                    CommitTimestamp = timestamp,
                    CommitHash = components[2],
                    CommitMessage = components[3]
                };
        }
    }
}
