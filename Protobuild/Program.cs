using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;

namespace Protobuild
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var needToExit = false;
            int exitCode = 0;
            var options = new Options();
            options["extract-xslt"] = x =>
            {
                if (Directory.Exists("Build"))
                {
                    using (var writer = new StreamWriter(Path.Combine("Build", "GenerateProject.xslt")))
                    {
                        Assembly.GetExecutingAssembly().GetManifestResourceStream(
                            "Protobuild.BuildResources.GenerateProject.xslt").CopyTo(writer.BaseStream);
                        writer.Flush();
                    }
                    using (var writer = new StreamWriter(Path.Combine("Build", "GenerateSolution.xslt")))
                    {
                        Assembly.GetExecutingAssembly().GetManifestResourceStream(
                            "Protobuild.BuildResources.GenerateSolution.xslt").CopyTo(writer.BaseStream);
                        writer.Flush();
                    }
                    needToExit = true;
                }
            };
            options["extract-proj"] = x =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    ResourceExtractor.ExtractProject(Path.Combine(Environment.CurrentDirectory, "Build"), module.Name);
                    needToExit = true;
                }
            };
            options["extract-util"] = x =>
            {
                if (Directory.Exists("Build"))
                {
                    ResourceExtractor.ExtractUtilities(Path.Combine(Environment.CurrentDirectory, "Build"));
                    needToExit = true;
                }
            };
            options["sync"] = x =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    Actions.Sync(module);
                    needToExit = true;
                }
            };
            options["resync"] = x =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    Actions.Resync(module);
                    needToExit = true;
                }
            };
            options["generate@1"] = x =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    exitCode = Actions.RegenerateProjects(module.Path, x.Length > 0 ? x[0] : null);
                    needToExit = true;
                }
            };
            options["clean@1"] = x =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    exitCode = Actions.CleanProjects(module.Path, x.Length > 0 ? x[0] : null);
                    needToExit = true;
                }
            };
            options["help"] = x =>
            {
                Console.WriteLine("Protobuild.exe [-extract-xslt] [-extract-proj] [-sync] [-resync] [-generate]");
                Console.WriteLine();
                Console.WriteLine("By default Protobuild starts a graphical interface for managing project");
                Console.WriteLine("definitions.  However, by using the command-line arguments it can be");
                Console.WriteLine("run in batch mode.");
                Console.WriteLine();
                Console.WriteLine("  -extract-xslt");
                Console.WriteLine();
                Console.WriteLine("  Extracts the XSLT templates to the Build\\ folder.  When present, these");
                Console.WriteLine("  are used over the built-in versions.  This allows you to customize");
                Console.WriteLine("  and extend the project / solution generation to support additional");
                Console.WriteLine("  features.");
                Console.WriteLine();
                Console.WriteLine("  -extract-proj");
                Console.WriteLine();
                Console.WriteLine("  Extracts the Main.proj file again to the Build\\ folder.  This is");
                Console.WriteLine("  useful if you have updated Protobuild and need to extract the");
                Console.WriteLine("  latest build script.");
                Console.WriteLine();
                Console.WriteLine("  -extract-util");
                Console.WriteLine();
                Console.WriteLine("  Extracts utilities again to the Build\\ folder.  This is");
                Console.WriteLine("  useful if you the utilities have been deleted from the Build\\");
                Console.WriteLine("  folder and you need to get them back.");
                Console.WriteLine();
                Console.WriteLine("  -sync");
                Console.WriteLine();
                Console.WriteLine("  Synchronises the changes in the C# project files back to the");
                Console.WriteLine("  definitions, but does not regenerate the projects.");
                Console.WriteLine();
                Console.WriteLine("  -resync");
                Console.WriteLine();
                Console.WriteLine("  Synchronises the changes in the C# project files back to the");
                Console.WriteLine("  definitions and then regenerates the projects again.");
                Console.WriteLine();
                Console.WriteLine("  -generate <platform>");
                Console.WriteLine();
                Console.WriteLine("  Generates the C# project files from the definitions.");
                Console.WriteLine();
                Console.WriteLine("  -clean <platform>");
                Console.WriteLine();
                Console.WriteLine("  Removes all generated C# project and solution files.");
                Console.WriteLine();
                needToExit = true;
            };
            options.Parse(args);
            if (needToExit)
            {
                Environment.Exit(exitCode);
                return;
            }

            // Check to see if we would be able to load GTK# assemblies.
            try
            {
                Assembly.Load("gtk-sharp, Version=2.4.0.0, Culture=neutral, PublicKeyToken=35e10195dab3c99f");
            }
            catch (BadImageFormatException)
            {
                // This is fine, it means that GTK# is installed, but because we're
                // not a 32-bit assembly, we can't actually load it.  Protobuild
                // Manager is a 32-bit assembly, so it will work fine.
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("GTK# is not installed.  Install GTK# " +
                                  "from http://www.go-mono.com/mono-downloads/download.html " +
                                  "and try again.");
                try
                {
                    MessageBox.Show("GTK# is not installed.  Install GTK# " +
                                    "from http://www.go-mono.com/mono-downloads/download.html " +
                                    "and try again.");
                }
                catch
                {
                    // Potentially no X server present.
                }
            }
        
            // Other we need to extract the ProtobuildManager to a temporary
            // directory and run it since it has to run as 32-bit.
            var random = new Random();
            var temporary = Path.Combine(Path.GetTempPath(), "Protobuild_" + random.Next());
            if (!Directory.Exists(temporary))
                Directory.CreateDirectory(temporary);
            var temporaryManager = Path.Combine(temporary, "ProtobuildManager.exe");
            var temporaryConsole = Path.Combine(temporary, "Protobuild.exe");
            File.Copy(Assembly.GetExecutingAssembly().Location, temporaryConsole);
            using (var b = new BinaryWriter(File.Open(temporaryManager, FileMode.Create)))
            {
                using (var m = Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Protobuild.ProtobuildManager.exe"))
                {
                    m.CopyTo(b.BaseStream);
                    b.Flush();
                }
            }
            // Attempt to set the executable bit if possible.  On platforms
            // where this doesn't work, it doesn't matter anyway.
            try
            {
                var pc = Process.Start("chmod", "a+x " + temporaryManager.Replace("\\", "\\\\").Replace(" ", "\\ "));
                pc.WaitForExit();
            }
            catch (Exception)
            {
            }
            var p = Process.Start(temporaryManager);
            p.WaitForExit();
            File.Delete(temporaryManager);
            File.Delete(temporaryConsole);
            Directory.Delete(temporary);
            Environment.Exit(p.ExitCode);
        }
    }
}
