using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Protobuild
{
    internal static class PathUtils
    {
        private static string _chmodPath = null;

        public static void MakePathExecutable(string path, bool wait)
        {
            try
            {
                if (Path.DirectorySeparatorChar != '/')
                {
                    // Not a UNIX system, don't worry about marking files as executable.
                    return;
                }

                if (_chmodPath == null)
                {
                    var chmods = new[]
                    {
                        "/bin/chmod",
                        "/usr/bin/chmod",
                        "/usr/local/bin/chmod"
                    };
                    _chmodPath = chmods.FirstOrDefault(File.Exists);

                    if (_chmodPath == null)
                    {
                        // No chmod command.
                        return;
                    }
                }

                var fileInfo = new FileInfo(path);

                var chmodStartInfo = new ProcessStartInfo
                {
                    FileName = _chmodPath,
                    Arguments = "a+x '" + fileInfo.Name.Replace("'", "'\"'\"'") + "'",
                    WorkingDirectory = fileInfo.DirectoryName,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var process = Process.Start(chmodStartInfo);
                if (wait)
                {
                    process.WaitForExit();
                }
            }
            catch
            {
                // Marking files as executable is always a best-effort process.
            }
        }

        public static string GetRelativePath(string absoluteFrom, string absoluteTo)
        {
            absoluteFrom = absoluteFrom.Replace('\\', '/');
            absoluteTo = absoluteTo.Replace('\\', '/');
            return (new Uri(absoluteFrom).MakeRelativeUri(new Uri(absoluteTo)))
                .ToString().Replace('/', '\\');
        }

        /// <remarks>
        /// Sourced from http://stackoverflow.com/questions/1157246/#answer-8521573.
        /// </remarks>
        public static void AggressiveDirectoryDelete(string directory)
        {
            if (!Path.IsPathRooted(directory))
            {
                throw new InvalidOperationException(
                    "Directory deletion requested without absolute path; this indicates " +
                    "a bug in Protobuild.");
            }

            File.SetAttributes(directory, FileAttributes.Normal);

            var files = Directory.GetFiles(directory);
            var dirs = Directory.GetDirectories(directory);

            foreach (var file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (var dir in dirs)
            {
                AggressiveDirectoryDelete(dir);
            }

            for (var i = 0; i < 5; i++)
            {
                try
                {
                    Directory.Delete(directory, true);
                    break;
                }
                catch (IOException ex)
                {
                    Thread.Sleep(500);
                    if (i == 4)
                    {
                        throw new IOException(
                            "Unable to delete the directory '" + directory + "' after 5 " +
                            "attempts.  Is it in-use by another process?", ex);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Thread.Sleep(500);
                    if (i == 4)
                    {
                        throw new UnauthorizedAccessException(
                            "Unable to delete the directory '" + directory + "' after 5 " +
                            "attempts.  Is it in-use by another process?", ex);
                    }
                }
            }
        }
    }
}

