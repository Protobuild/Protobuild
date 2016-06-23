using System;
using System.IO;
using System.Threading;

namespace Protobuild
{
    internal static class PathUtils
    {
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

