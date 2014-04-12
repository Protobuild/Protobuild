using System;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Protobuild
{
    using System.ComponentModel;
    using Microsoft.Win32;

    public class JSILProvider
    {   
        public bool GetJSIL(out string jsilDirectory, out string jsilCompilerFile)
        {
            if (File.Exists(this.GetJSILCompilerPath()))
            {
                jsilDirectory = this.GetJSILRuntimeDirectory();
                jsilCompilerFile = this.GetJSILCompilerPath();
                return true;
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

        private string GetJSILDirectory(string type)
        {
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var jsil = Path.Combine(appdata, ".jsil", type);
            if (!Directory.Exists(jsil))
            {
                Directory.CreateDirectory(jsil);
            }
            return jsil;
        }

        private string GetJSILSourceDirectory()
        {
            return this.GetJSILDirectory("source");
        }

        private string GetJSILRuntimeDirectory()
        {
            return this.GetJSILDirectory("runtime");
        }

        private string GetJSILCompilerPath()
        {
            return Path.Combine(this.GetJSILRuntimeDirectory(), "JSILc.exe");
        }

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

        private bool DetectBuilder(out string pathOrError)
        {
            if (Actions.DetectPlatform() == "Windows")
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
                        var msBuildToolsBasePath =
                            (string)Registry.LocalMachine.OpenSubKey("SOFTWARE")
                                .OpenSubKey("Microsoft")
                                .OpenSubKey("MSBuild")
                                .OpenSubKey("ToolsVersions")
                                .OpenSubKey("4.0")
                                .GetValue("MSBuildToolsPath");
                        var msBuildPath = Path.Combine(msBuildToolsBasePath, "msbuild.exe");
                        if (File.Exists(msBuildPath))
                        {
                            pathOrError = msBuildPath;
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
    }
}

