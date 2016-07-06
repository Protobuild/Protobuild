using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Protobuild
{
    internal static class GitUtils
    {
        private static string _cachedGitPath;

        private static void RunGitInternal(string str, string workingDirectory, string consoleWriteLine)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = GetCachedGitPath(),
                Arguments = str,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true
            };
            
            Console.WriteLine(consoleWriteLine);
            
            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Unable to execute Git!");
            }

            process.StandardInput.Close();
            process.WaitForExit();

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
                    FileName = GetCachedGitPath(),
                    Arguments = str,
                    WorkingDirectory = folder == null ? Environment.CurrentDirectory : Path.Combine(Environment.CurrentDirectory, folder),
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };

            var suffix = folder == null ? "" : " (" + folder + ")";
            Console.WriteLine("Executing: git " + str + suffix);

            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Unable to execute Git!");
            }

            process.StandardInput.Close();
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
                    FileName = GetCachedGitPath(),
                    Arguments = "rev-parse --is-inside-work-tree",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                };

            Console.WriteLine("Executing: git rev-parse --is-inside-work-tree");
            var process = Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Unable to execute Git!");
            }
            process.StandardInput.Close();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // If we are not in a Git directory at all, the process exits with
                // an error code of 128.
                return false;
            }

            if (process.StandardOutput.ReadToEnd().Trim() == "true")
            {
                // If we are inside a working tree (not a .git folder), this outputs
                // "true" with an exit code of 0.
                return true;
            }

            // If we are inside a .git folder (not a working tree), this outputs
            // "false" with an exit code of 0.
            return false;
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

        private static string GetCachedGitPath()
        {
            if (_cachedGitPath == null)
            {
                _cachedGitPath = FindGitOnSystemPath();
            }

            return _cachedGitPath;
        }

        private static string FindGitOnSystemPath()
        {
            if (Path.DirectorySeparatorChar != '/')
            {
                // We're on Windows.  We split the environment PATH to see if we
                // can find Git, then we check standard directories (like
                // C:\Program Files (x86)\Git) etc.
                var pathEnv = Environment.GetEnvironmentVariable("PATH");
                var paths = new string[0];
                if (pathEnv != null)
                {
                    paths = pathEnv.Split(';');
                }

                var standardPaths = new List<string>
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "cmd"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Git", "bin"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "cmd"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git", "bin"),
                };

                // Add standard paths that GitHub for Windows uses.  Because the file
                // contains a hash, or some other mutable component, we need to search for
                // the PortableGit path.
                var github = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GitHub");
                if (Directory.Exists(github))
                {
                    foreach (var subfolder in new DirectoryInfo(github).GetDirectories())
                    {
                        if (subfolder.Name.StartsWith("PortableGit_"))
                        {
                            standardPaths.Add(Path.Combine(subfolder.FullName, "cmd"));
                        }
                    }
                }

                var filenames = new[] {"git.exe", "git.bat", "git.cmd"};
                foreach (var path in paths.Concat(standardPaths))
                {
                    foreach (var filename in filenames)
                    {
                        if (File.Exists(Path.Combine(path, filename)))
                        {
                            // We found Git.
                            return Path.Combine(path, filename);
                        }
                    }
                }

                Console.Error.WriteLine(
                    "WARNING: Unable to find Git on your PATH, or any standard " +
                    "locations.  Have you installed Git on this system?");
                return "git";
            }

            // For UNIX systems, Git should always be on the PATH.
            return "git";
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
				var path = Path.Combine(root, ".git", "info", "exclude");
				Directory.CreateDirectory(Path.GetDirectoryName(path));
				return path;
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

