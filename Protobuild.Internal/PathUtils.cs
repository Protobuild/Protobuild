using System;
using System.IO;

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

            Directory.Delete(directory, false);
        }
    }
}

