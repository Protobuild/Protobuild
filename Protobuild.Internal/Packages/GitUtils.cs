using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    public static class GitUtils
    {
        private static void RunGitInternal(string str, string workingDirectory, string consoleWriteLine)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = str,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            
            Console.WriteLine(consoleWriteLine);

            var renderer = new GitRenderer();
            DataReceivedEventHandler handler = (sender, args) =>
            {
                renderer.Update(args.Data);
            };
            
            var process = Process.Start(processStartInfo);
            process.OutputDataReceived += handler;
            process.ErrorDataReceived += handler;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            renderer.Finalize();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Got an unexpected exit code of " + process.ExitCode + " from Git");
            }
        }

        public static void RunGit(string folder, string str)
        {
            var suffix = folder == null ? "" : " (" + folder + ")";
            RunGitInternal(str,
                folder == null ? Environment.CurrentDirectory : Path.Combine(Environment.CurrentDirectory, folder),
                "Executing: git " + str + suffix);
        }

        public static void RunGitAbsolute(string folder, string str)
        {
            RunGitInternal(str,
                folder,
                "Executing: git " + str + " (in " + folder + ")");
        }

        public static string RunGitAndCapture(string folder, string str)
        {
            var processStartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = str,
                    WorkingDirectory = folder == null ? Environment.CurrentDirectory : Path.Combine(Environment.CurrentDirectory, folder),
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };

            var suffix = folder == null ? "" : " (" + folder + ")";
            Console.WriteLine("Executing: git " + str + suffix);

            var process = Process.Start(processStartInfo);
            var result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Got an unexpected exit code of " + process.ExitCode + " from Git");
            }

            return result;
        }

        public static bool IsGitRepository()
        {
            var processStartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

            var process = Process.Start(processStartInfo);
            process.WaitForExit();

            if (process.ExitCode == 128)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void UnmarkIgnored(string folder)
        {
            var excludePath = GitUtils.GetGitExcludePath(Path.Combine(folder, ".."));

            if (excludePath == null)
            {
                return;
            }

            var contents = GitUtils.GetFileStringList(excludePath).ToList();
            contents.Remove(folder);
            GitUtils.SetFileStringList(excludePath, contents);
        }

        public static void MarkIgnored(string folder)
        {
            var excludePath = GitUtils.GetGitExcludePath(Path.Combine(folder, ".."));

            if (excludePath == null)
            {
                return;
            }

            var contents = GitUtils.GetFileStringList(excludePath).ToList();
            contents.Add(folder);
            GitUtils.SetFileStringList(excludePath, contents);
        }

        private static string GetGitExcludePath(string folder)
        {
            var root = GitUtils.GetGitRootPath(folder);

            if (root == null)
            {
                return null;
            }
            else 
            {
                return Path.Combine(root, ".git", "info", "exclude");
            }
        }

        private static string GetGitRootPath(string folder)
        {
            var current = folder;

            while (current != null && !Directory.Exists(Path.Combine(folder, ".git")))
            {
                var parent = new DirectoryInfo(current).Parent;

                if (parent == null)
                {
                    current = null;
                }
                else 
                {
                    current = parent.FullName;
                }
            }

            return current;
        }

        private static void SetFileStringList(string excludePath, IEnumerable<string> contents)
        {
            using (var writer = new StreamWriter(excludePath, false))
            {
                foreach (var line in contents)
                {
                    writer.WriteLine(line);
                }
            }
        }

        private static IEnumerable<string> GetFileStringList(string excludePath)
        {
            var results = new List<string>();

            using (var reader = new StreamReader(excludePath))
            {
                while (!reader.EndOfStream)
                {
                    results.Add(reader.ReadLine());
                }
            }

            return results;
        }
    }
}

