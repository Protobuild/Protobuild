namespace Protobuild
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Provides services for JSIL, such as downloading and installation of JSIL.
    /// </summary>
    /// <remarks>
    /// This is used by Protobuild to automatically download and build JSIL when the user
    /// first targets the Web platform.
    /// </remarks>
    internal class JSILProvider : IJSILProvider
    {
        private readonly IKnownToolProvider _knownToolProvider;

        public JSILProvider(IKnownToolProvider knownToolProvider)
        {
            _knownToolProvider = knownToolProvider;
        }

        /// <summary>
        /// Returns the required JSIL directories, downloading and building JSIL if necessary.
        /// </summary>
        /// <remarks>
        /// If this returns <c>false</c>, then an error was encountered while downloading or
        /// building JSIL.
        /// </remarks>
        /// <returns><c>true</c>, if JSIL was available or was installed successfully, <c>false</c> otherwise.</returns>
        /// <param name="jsilDirectory">The runtime directory of JSIL.</param>
        /// <param name="jsilCompilerFile">The JSIL compiler executable.</param>
        public bool GetJSIL(out string jsilDirectory, out string jsilCompilerFile)
        {
            jsilCompilerFile = _knownToolProvider.GetToolExecutablePath("JSILc");
            if (jsilCompilerFile == null)
            {
                jsilDirectory = null;
                return false;
            }

            jsilDirectory = new FileInfo(jsilCompilerFile).Directory.FullName;
            return true;
        }

        /// <summary>
        /// Gets a list of JSIL runtime libraries (i.e. the Javascript files), so they can
        /// be included in the projects as copy-on-output.
        /// </summary>
        /// <returns>The JSIL libraries to include in the project.</returns>
        public IEnumerable<KeyValuePair<string, string>> GetJSILLibraries()
        {
            return this.ScanFolder(Path.Combine(this.GetJSILRuntimeDirectory(), "Libraries"), string.Empty);
        }

        /// <summary>
        /// Gets the JSIL runtime directory.
        /// </summary>
        /// <returns>The JSIL runtime directory.</returns>
        private string GetJSILRuntimeDirectory()
        {
            var jsilCompilerFile = _knownToolProvider.GetToolExecutablePath("JSILc");
            if (jsilCompilerFile == null)
            {
                throw new InvalidOperationException("JSIL is not installed.");
            }

            return new FileInfo(jsilCompilerFile).Directory.FullName;
        }

        /// <summary>
        /// Recursively scans the specified folder, returning all of the entries as
        /// original path, new path pairs.
        /// </summary>
        /// <returns>The key value pairs of files that were scanned.</returns>
        /// <param name="path">The path to scan.</param>
        /// <param name="name">The "destination" path, under which the files are mapped.</param>
        private IEnumerable<KeyValuePair<string, string>> ScanFolder(string path, string name)
        {
            var dirInfo = new DirectoryInfo(path);

            foreach (var file in dirInfo.GetFiles())
            {
                yield return new KeyValuePair<string, string>(file.FullName, Path.Combine(name, file.Name));
            }

            foreach (var dir in dirInfo.GetDirectories())
            {
                foreach (var entry in this.ScanFolder(dir.FullName, Path.Combine(name, dir.Name)))
                {
                    yield return entry;
                }
            }
        }
    }
}
