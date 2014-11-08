//-----------------------------------------------------------------------
// <copyright file="JSILProvider.cs" company="Protobuild Project">
// The MIT License (MIT)
// 
// Copyright (c) Various Authors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//     The above copyright notice and this permission notice shall be included in
//     all copies or substantial portions of the Software.
// 
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//     THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Protobuild
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using Microsoft.Win32;

    /// <summary>
    /// Provides services for JSIL, such as downloading and installation of JSIL.
    /// </summary>
    /// <remarks>
    /// This is used by Protobuild to automatically download and build JSIL when the user
    /// first targets the Web platform.
    /// </remarks>
    public class JSILProvider : IJSILProvider
    {
        private readonly IHostPlatformDetector m_HostPlatformDetector;

        public JSILProvider(IHostPlatformDetector hostPlatformDetector)
        {
            this.m_HostPlatformDetector = hostPlatformDetector;
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
            if (File.Exists(this.GetJSILCompilerPath()))
            {
                jsilDirectory = this.GetJSILRuntimeDirectory();
                jsilCompilerFile = this.GetJSILCompilerPath();
                return true;
            }

            if (this.BuggyMonoDetected())
            {
                Console.WriteLine("=============== Please update Mono ===============");
                Console.WriteLine("Mono 3.2.6 is known to be buggy when building ");
                Console.WriteLine("JSIL.  To update Mono, upgrade via your package ");
                Console.WriteLine("manager on Linux, or if you are on Mac update Mono ");
                Console.WriteLine("by downloading the latest version from: ");
                Console.WriteLine();
                Console.WriteLine("  http://www.go-mono.com/mono-downloads/download.html");
                Console.WriteLine();
                Console.WriteLine("=============================================================");
                jsilDirectory = null;
                jsilCompilerFile = null;
                return false;
            }

            Console.WriteLine("=============== JSIL runtime is not installed ===============");
            Console.WriteLine("I will now download and build JSIL for the Web platform.");
            Console.WriteLine("Installing into: " + this.GetJSILRuntimeDirectory());

            Console.Write("Removing existing JSIL runtime... ");
            try
            {
                Directory.Delete(this.GetJSILSourceDirectory(), true);
                Directory.Delete(this.GetJSILRuntimeDirectory(), true);
                Console.WriteLine("done.");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("error!");
                Console.WriteLine("Unable to remove existing JSIL runtime or source.  Remove");
                Console.WriteLine("the files and directories located in the following directory:");
                Console.WriteLine();
                Console.WriteLine("  " + this.GetJSILDirectory(string.Empty));
                Console.WriteLine();
                Console.WriteLine("and then try again.");
                Console.WriteLine();
                Console.WriteLine("=============================================================");
                jsilDirectory = null;
                jsilCompilerFile = null;
                return false;
            }

            Console.WriteLine("Downloading JSIL source code via Git... ");

            try
            {
                this.ExecuteGit("clone https://github.com/sq/JSIL.git .");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("error!");
                Console.WriteLine("You don't have Git installed or it currently isn't in your PATH.");
                Console.WriteLine("JSIL has to be downloaded via Git due to the use of submodules.");
                Console.WriteLine("=============================================================");
                jsilDirectory = null;
                jsilCompilerFile = null;
                return false;
            }

            Console.WriteLine("Updating JSIL submodules... ");
            this.ExecuteGit("submodule update --init --recursive");
            Console.WriteLine("done.");

            Console.WriteLine("Patching JSIL project files... ");
            this.PatchFile(
                Path.Combine(this.GetJSILSourceDirectory(), "Compiler", "Compiler.csproj"),
                x => 
                Regex.Replace(
                    Regex.Replace(
                        x, 
                        "\\<PreBuildEvent\\>[^\\<]+\\<\\/PreBuildEvent\\>",
                        string.Empty),
                    "\\<PostBuildEvent\\>[^\\<]+\\<\\/PostBuildEvent\\>",
                    string.Empty));
            this.PatchFile(
                Path.Combine(
                    this.GetJSILSourceDirectory(), 
                    "Upstream", 
                    "ILSpy", 
                    "ICSharpCode.Decompiler", 
                    "ICSharpCode.Decompiler.csproj"),
                x => 
                Regex.Replace(
                    Regex.Replace(
                        x,
                        "<Compile Include=\"Properties\\\\AssemblyInfo\\.cs\" />", 
                        string.Empty),
                    "<Target Name=\"BeforeBuild\">(.+?)</Target>", 
                    string.Empty,
                    RegexOptions.Singleline));
            Console.WriteLine("done.");

            Console.WriteLine("Building JSIL compiler... ");
            string builderOrError;
            if (!this.DetectBuilder(out builderOrError))
            {
                Console.WriteLine("error!");
                Console.WriteLine("Unable to locate the tool to build C# projects.  The exact ");
                Console.WriteLine("error is: ");
                Console.WriteLine();
                Console.WriteLine("  " + builderOrError);
                Console.WriteLine();
                Console.WriteLine("If you are running on Windows, make sure you are running ");
                Console.WriteLine("Protobuild under the Visual Studio command prompt.");
                Console.WriteLine();
                Console.WriteLine("=============================================================");
                jsilDirectory = null;
                jsilCompilerFile = null;
                return false;
            }

            if (!this.ExecuteProgram(builderOrError, "Compiler" + Path.DirectorySeparatorChar + "Compiler.csproj"))
            {
                Console.WriteLine("error!");
                Console.WriteLine("Unable to build the JSIL compiler.  This may be caused by an ");
                Console.WriteLine("incompatible change made in the upstream codebase.  Things you ");
                Console.WriteLine("can try:");
                Console.WriteLine();
                Console.WriteLine(" 1) Make sure you have the latest version of Protobuild");
                Console.WriteLine(" 2) If the problem still persists, file an issue at");
                Console.WriteLine("    https://github.com/hach-que/Protobuild/issues");
                Console.WriteLine("    with a copy of the above output.");
                Console.WriteLine();
                Console.WriteLine("=============================================================");
                jsilDirectory = null;
                jsilCompilerFile = null;
                return false;
            }

            Console.WriteLine("Copying resulting binaries... ");
            foreach (var file in new DirectoryInfo(Path.Combine(this.GetJSILSourceDirectory(), "bin")).GetFiles())
            {
                Console.WriteLine("> " + file.Name);
                file.CopyTo(Path.Combine(this.GetJSILRuntimeDirectory(), file.Name));
            }

            Console.WriteLine("done.");

            Console.WriteLine("Creating runtime libraries... ");
            Directory.CreateDirectory(Path.Combine(this.GetJSILRuntimeDirectory(), "Libraries"));
            Console.WriteLine("done.");

            Console.WriteLine("Copying runtime libraries... ");
            this.RecursiveCopy(
                Path.Combine(this.GetJSILSourceDirectory(), "Libraries"),
                Path.Combine(this.GetJSILRuntimeDirectory(), "Libraries"));
            Console.WriteLine("done.");

            Console.Write("Removing temporary build directory... ");
            try
            {
                Directory.Delete(this.GetJSILSourceDirectory(), true);
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore
            }

            Console.WriteLine("done.");

            if (File.Exists(this.GetJSILCompilerPath()))
            {
                jsilDirectory = this.GetJSILRuntimeDirectory();
                jsilCompilerFile = this.GetJSILCompilerPath();
                return true;
            }
            else
            {
                Console.WriteLine("error.");
                Console.WriteLine("The build did not result in a JSILc.exe file being present ");
                Console.WriteLine("at: " + this.GetJSILCompilerPath());
                Console.WriteLine();
                Console.WriteLine("=============================================================");
                jsilDirectory = null;
                jsilCompilerFile = null;
                return false;
            }
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
        /// Recursively copies files from the source directory to the destination.
        /// </summary>
        /// <param name="source">The source directory.</param>
        /// <param name="destination">The destination directory.</param>
        private void RecursiveCopy(string source, string destination)
        {
            foreach (var file in new DirectoryInfo(source).GetFiles())
            {
                Console.WriteLine("> " + file.Name);
                file.CopyTo(Path.Combine(destination, file.Name));
            }

            foreach (var directory in new DirectoryInfo(source).GetDirectories())
            {
                if (!Directory.Exists(Path.Combine(destination, directory.Name)))
                {
                    Directory.CreateDirectory(Path.Combine(destination, directory.Name));
                }

                this.RecursiveCopy(Path.Combine(source, directory.Name), Path.Combine(destination, directory.Name));
            }
        }

        /// <summary>
        /// Gets one of the JSIL directories, either "runtime" or "source".
        /// </summary>
        /// <remarks>
        /// This defaults to %appdata%\.jsil\{type}, or the JSIL_DIRECTORY environment variable
        /// if it is set (with {type} as a subdirectory).
        /// </remarks>
        /// <returns>The JSIL root directory.</returns>
        /// <param name="type">The type of directory to return (either "runtime" or "source").</param>
        private string GetJSILDirectory(string type)
        {
            var targetdir = Environment.GetEnvironmentVariable("JSIL_DIRECTORY");
            if (string.IsNullOrEmpty(targetdir))
            {
                targetdir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".jsil");
            }

            var jsil = Path.Combine(targetdir, type);
            if (!Directory.Exists(jsil))
            {
                Directory.CreateDirectory(jsil);
            }

            return jsil;
        }

        /// <summary>
        /// Gets the JSIL source directory.
        /// </summary>
        /// <returns>The JSIL source directory.</returns>
        private string GetJSILSourceDirectory()
        {
            return this.GetJSILDirectory("source");
        }

        /// <summary>
        /// Gets the JSIL runtime directory.
        /// </summary>
        /// <returns>The JSIL runtime directory.</returns>
        private string GetJSILRuntimeDirectory()
        {
            return this.GetJSILDirectory("runtime");
        }

        /// <summary>
        /// Gets the path to the JSIL compiler executable.
        /// </summary>
        /// <returns>The path to JSIL compiler executable.</returns>
        private string GetJSILCompilerPath()
        {
            return Path.Combine(this.GetJSILRuntimeDirectory(), "JSILc.exe");
        }

        /// <summary>
        /// Executes Git to clone or update the source files for JSIL.
        /// </summary>
        /// <param name="args">The arguments to pass to Git.</param>
        private void ExecuteGit(string args)
        {
            Process process;
            try
            {
                process =
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = "git",
                            Arguments = args,
                            UseShellExecute = false,
                            WorkingDirectory = Path.Combine(this.GetJSILSourceDirectory())
                        });
                process.WaitForExit();
            }
            catch (Win32Exception)
            {
                throw new InvalidOperationException();
            }

            if (process.ExitCode == 127)
            {
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Executes a program, such as MSBuild during the build or installation of JSIL.
        /// </summary>
        /// <returns><c>true</c>, if program had a successful exit code, <c>false</c> otherwise.</returns>
        /// <param name="name">The full path to the program.</param>
        /// <param name="args">The arguments to the program.</param>
        private bool ExecuteProgram(string name, string args)
        {
            Process process;
            try
            {
                process =
                    Process.Start(
                        new ProcessStartInfo
                        {
                            FileName = name,
                            Arguments = args,
                            UseShellExecute = false,
                            WorkingDirectory = Path.Combine(this.GetJSILSourceDirectory())
                        });
                process.WaitForExit();
            }
            catch (Win32Exception)
            {
                throw new InvalidOperationException();
            }

            if (process.ExitCode == 127)
            {
                throw new InvalidOperationException();
            }

            return process.ExitCode == 0;
        }

        /// <summary>
        /// Patches the file by passing it's contents through the specified modification function.
        /// </summary>
        /// <param name="path">The file to modify.</param>
        /// <param name="modify">The function to modify the file's text.</param>
        private void PatchFile(string path, Func<string, string> modify)
        {
            string projectText;
            using (var reader = new StreamReader(path))
            {
                projectText = reader.ReadToEnd();
            }

            projectText = modify(projectText);

            using (var writer = new StreamWriter(path))
            {
                writer.Write(projectText);
            }
        }

        /// <summary>
        /// Detects the appropriate build tool, either MSBuild or xbuild.
        /// </summary>
        /// <returns><c>true</c>, if build tool was detected, <c>false</c> otherwise.</returns>
        /// <param name="pathOrError">The path of the build tool if successful, or the error message on failure.</param>
        private bool DetectBuilder(out string pathOrError)
        {
            if (this.m_HostPlatformDetector.DetectPlatform() == "Windows")
            {
                try
                {
                    this.ExecuteProgram("msbuild", "/?");
                    pathOrError = "msbuild";
                    return true;
                }
                catch (InvalidOperationException)
                {
                    try
                    {
                        var msbuildToolsBasePath =
                            (string)Registry.LocalMachine.OpenSubKey("SOFTWARE")
                                .OpenSubKey("Microsoft")
                                .OpenSubKey("MSBuild")
                                .OpenSubKey("ToolsVersions")
                                .OpenSubKey("4.0")
                                .GetValue("MSBuildToolsPath");
                        var msbuildPath = Path.Combine(msbuildToolsBasePath, "msbuild.exe");
                        if (File.Exists(msbuildPath))
                        {
                            pathOrError = msbuildPath;
                            return true;
                        }
                    }
                    catch
                    {
                    }

                    pathOrError = "msbuild is not in your PATH";
                    return false;
                }
            }
            else
            {
                try
                {
                    this.ExecuteProgram("xbuild", "/?");
                    pathOrError = "xbuild";
                    return true;
                }
                catch (InvalidOperationException)
                {
                    pathOrError = "xbuild is not in your PATH";
                    return false;
                }
            }
        }

        /// <summary>
        /// Detects if the user is currently running a buggy version of Mono, under which
        /// JSIL will not build successfully.
        /// </summary>
        /// <returns><c>true</c>, if the user is running a buggy version of Mono, <c>false</c> otherwise.</returns>
        private bool BuggyMonoDetected()
        {
            string builder;
            if (!this.DetectBuilder(out builder))
            {
                // Can't detect build system, will fail later.
                return false;
            }

            if (builder != "xbuild")
            {
                // MSBuild available, so we aren't running Mono.
                return false;
            }

            try
            {
                var processStartInfo = new ProcessStartInfo();
                processStartInfo.FileName = "mono";
                processStartInfo.Arguments = "--version";
                processStartInfo.RedirectStandardOutput = true;
                processStartInfo.UseShellExecute = false;
                var process = Process.Start(processStartInfo);
                var text = process.StandardOutput.ReadToEnd();

                if (text.Contains("version 3.2.6"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
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
