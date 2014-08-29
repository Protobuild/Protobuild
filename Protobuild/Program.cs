using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;

namespace Protobuild
{
    using System.Collections.Generic;
    using System.IO.Compression;

    class MainClass
    {
        public static void Main(string[] args)
        {
            var needToExit = false;
            var runModuleManager = false;
            int exitCode = 0;
            var options = new Options();
            var enabledServices = new List<string>();
            var disabledServices = new List<string>();
            string serviceSpecPath = null;
            Action actionHandler = null;

            options["extract-xslt"] = x =>
            {
                if (actionHandler != null)
                {
                    throw new InvalidOperationException("More than one action option specified.");
                }

                actionHandler = () =>
                {
                    if (Directory.Exists("Build"))
                    {
                        using (var writer = new StreamWriter(Path.Combine("Build", "GenerateProject.xslt")))
                        {
                            ResourceExtractor.GetTransparentDecompressionStream(
                                Assembly.GetExecutingAssembly()
                                    .GetManifestResourceStream("Protobuild.BuildResources.GenerateProject.xslt.gz"))
                                .CopyTo(writer.BaseStream);
                            writer.Flush();
                        }
                        using (var writer = new StreamWriter(Path.Combine("Build", "GenerateSolution.xslt")))
                        {
                            ResourceExtractor.GetTransparentDecompressionStream(
                                Assembly.GetExecutingAssembly()
                                    .GetManifestResourceStream("Protobuild.BuildResources.GenerateSolution.xslt.gz"))
                                .CopyTo(writer.BaseStream);
                            writer.Flush();
                        }
                        needToExit = true;
                    }
                };
            };
            options["sync@1"] = x =>
            {
                if (actionHandler != null)
                {
                    throw new InvalidOperationException("More than one action option specified.");
                }

                actionHandler = () =>
                {
                    if (Directory.Exists("Build"))
                    {
                        var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                        exitCode = Actions.PerformAction(
                            module,
                            "sync",
                            x.Length > 0 ? x[0] : null,
                            enabledServices.ToArray(),
                            disabledServices.ToArray(),
                            serviceSpecPath)
                                       ? 0
                                       : 1;
                        needToExit = true;
                    }
                };
            };
            options["resync@1"] = x =>
            {
                if (actionHandler != null)
                {
                    throw new InvalidOperationException("More than one action option specified.");
                }

                actionHandler = () =>
                {
                    if (Directory.Exists("Build"))
                    {
                        var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                        exitCode = Actions.PerformAction(
                            module,
                            "resync",
                            x.Length > 0 ? x[0] : null,
                            enabledServices.ToArray(),
                            disabledServices.ToArray(),
                            serviceSpecPath)
                                       ? 0
                                       : 1;
                        needToExit = true;
                    }
                };
            };
            options["generate@1"] = x =>
            {
                if (actionHandler != null)
                {
                    throw new InvalidOperationException("More than one action option specified.");
                }

                actionHandler = () =>
                {
                    if (Directory.Exists("Build"))
                    {
                        var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                        exitCode = Actions.PerformAction(
                            module,
                            "generate",
                            x.Length > 0 ? x[0] : null,
                            enabledServices.ToArray(),
                            disabledServices.ToArray(),
                            serviceSpecPath)
                                       ? 0
                                       : 1;
                        needToExit = true;
                    }
                };
            };
            options["clean@1"] = x =>
            {
                if (actionHandler != null)
                {
                    throw new InvalidOperationException("More than one action option specified.");
                }

                actionHandler = () =>
                {
                    if (Directory.Exists("Build"))
                    {
                        var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                        exitCode = Actions.PerformAction(
                            module,
                            "clean",
                            x.Length > 0 ? x[0] : null,
                            enabledServices.ToArray(),
                            disabledServices.ToArray(),
                            serviceSpecPath)
                                       ? 0
                                       : 1;
                        needToExit = true;
                    }
                };
            };
            options["compress@1"] = x =>
            {
                if (actionHandler != null)
                {
                    throw new InvalidOperationException("More than one action option specified.");
                }

                actionHandler = () =>
                {
                    var file = x.Length > 0 ? x[0] : null;
                    if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
                    {
                        Console.Error.WriteLine("File not found for compression.");
                        exitCode = 1;
                        needToExit = true;
                        return;
                    }

                    using (var reader = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        using (var writer = new FileStream(file + ".gz", FileMode.Create, FileAccess.Write))
                        {
                            using (var gzip = new GZipStream(writer, CompressionMode.Compress, true))
                            {
                                reader.CopyTo(gzip);
                            }
                        }
                    }

                    Console.WriteLine(file + " compressed as " + file + ".gz");
                    exitCode = 0;
                    needToExit = true;
                };
            };
            options["enable@1"] = x =>
            {
                if (x.Length == 0 || x[0] == null)
                {
                    throw new InvalidOperationException("You must provide an argument to the -enable option");
                }

                enabledServices.Add(x[0]);
            };
            options["disable@1"] = x =>
            {
                if (x.Length == 0 || x[0] == null)
                {
                    throw new InvalidOperationException("You must provide an argument to the -disable option");
                }

                disabledServices.Add(x[0]);
            };
            options["spec@1"] = x =>
            {
                if (x.Length == 0 || x[0] == null)
                {
                    throw new InvalidOperationException("You must provide an argument to the -spec option");
                }

                if (serviceSpecPath != null)
                {
                    throw new InvalidOperationException("Multiple -spec options passed.");
                }

                serviceSpecPath = x[0];
            };
            options["add@1"] = x =>
            {
                if (x.Length == 0 || x[0] == null)
                {
                    throw new InvalidOperationException("You must provide an argument to the -add option");
                }

                Actions.AddSubmodule(ModuleInfo.Load(Path.Combine("Build", "Module.xml")), x[0]);
            };
            options["manager-gui"] = x =>
            {
                if (actionHandler != null)
                {
                    throw new InvalidOperationException("More than one action option specified.");
                }

                actionHandler = () =>
                { runModuleManager = true; };
            };
            Action<string[]> helpAction = x =>
            {
                Console.WriteLine("Protobuild.exe [-extract-xslt] [-extract-proj] [-sync] [-resync] [-generate]");
                Console.WriteLine();
                Console.WriteLine("By default Protobuild does resynchronises or generates projects for");
                Console.WriteLine("the current platform.  To start a graphical interface for managing project");
                Console.WriteLine("definitions, use the -manager-gui option.");
                Console.WriteLine();
                Console.WriteLine("  -manager-gui");
                Console.WriteLine();
                Console.WriteLine("  Starts the module manager GUI.  You will need to have a graphical");
                Console.WriteLine("  system present in order to use this option; otherwise Protobuild will");
                Console.WriteLine("  operate as a console application (safe for headless operation).");
                Console.WriteLine();
                Console.WriteLine("  -extract-xslt");
                Console.WriteLine();
                Console.WriteLine("  Extracts the XSLT templates to the Build\\ folder.  When present, these");
                Console.WriteLine("  are used over the built-in versions.  This allows you to customize");
                Console.WriteLine("  and extend the project / solution generation to support additional");
                Console.WriteLine("  features.");
                Console.WriteLine();
                Console.WriteLine("  -extract-util");
                Console.WriteLine();
                Console.WriteLine("  Extracts utilities again to the Build\\ folder.  This is");
                Console.WriteLine("  useful if you the utilities have been deleted from the Build\\");
                Console.WriteLine("  folder and you need to get them back.");
                Console.WriteLine();
                Console.WriteLine("  -sync <platform>");
                Console.WriteLine();
                Console.WriteLine("  Synchronises the changes in the C# project files back to the");
                Console.WriteLine("  definitions, but does not regenerate the projects.  If no");
                Console.WriteLine("  platform is specified, synchronises for the current platform.");
                Console.WriteLine();
                Console.WriteLine("  -resync <platform>");
                Console.WriteLine();
                Console.WriteLine("  Synchronises the changes in the C# project files back to the");
                Console.WriteLine("  definitions and then regenerates the projects again.  If no");
                Console.WriteLine("  platform is specified, resynchronises for the current platform.");
                Console.WriteLine();
                Console.WriteLine("  -generate <platform>");
                Console.WriteLine();
                Console.WriteLine("  Generates the C# project files from the definitions.  If no");
                Console.WriteLine("  platform is specified, generates for the current platform.");
                Console.WriteLine();
                Console.WriteLine("  -clean <platform>");
                Console.WriteLine();
                Console.WriteLine("  Removes all generated C# project and solution files.  If no");
                Console.WriteLine("  platform is specified, cleans for the current platform.");
                Console.WriteLine();
                Console.WriteLine("  -enable <service>");
                Console.WriteLine();
                Console.WriteLine("  Enables the specified service.");
                Console.WriteLine();
                Console.WriteLine("  -disable <service>");
                Console.WriteLine();
                Console.WriteLine("  Disables the specified service.");
                Console.WriteLine();
                Console.WriteLine("  -spec <path>");
                Console.WriteLine();
                Console.WriteLine("  Internally used to pass the service specification.");
                Console.WriteLine();
                Console.WriteLine("  -add <url>");
                Console.WriteLine();
                Console.WriteLine("  Add a package or submodule URL to the current module.");
                Console.WriteLine("  WARNING: This feature is currently experimental.");
                Console.WriteLine();
                needToExit = true;
            };
            options["help"] = helpAction;
            options["?"] = helpAction;

            try
            {
                options.Parse(args);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                helpAction(new string[0]);
                needToExit = true;
                exitCode = 1;
            }

            if (actionHandler != null)
            {
                actionHandler();
            }

            if (needToExit)
            {
                Environment.Exit(exitCode);
                return;
            }

            if (!File.Exists(Path.Combine("Build", "Module.xml")))
            {
                try
                {
                    // We use Windows Forms here to prompt the user on what to do.
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    var form = new KickstartForm();
                    var result = form.ShowDialog();
                    switch (result)
                    {
                        case DialogResult.Yes:
                            RunModuleManager();
                            return;
                        case DialogResult.No:
                            Directory.CreateDirectory("Build");
                            ResourceExtractor.ExtractAll(Path.Combine(Environment.CurrentDirectory, "Build"), "MyProject");
                            MessageBox.Show("Build" + Path.DirectorySeparatorChar + "Module.xml has been created.");
                            Environment.Exit(0);
                            break;
                        default:
                            Environment.Exit(1);
                            break;
                    }
                }
                catch (Exception)
                {
                    // We might be on a platform that isn't running a graphical interface
                    // or we can't otherwise show a dialog to the user.  Output to the
                    // console to let them know what's going on.
                    Console.WriteLine("The current directory does not appear to be a Protobuild module.  Please either create ");
                    Console.WriteLine("the Module.xml file manually, or run 'Protobuild.exe --manager-gui' to be guided through ");
                    Console.WriteLine("the process of creating a new module.");
                    Environment.Exit(1);
                }
            }

            if (runModuleManager) RunModuleManager();
            else
                Actions.DefaultAction(
                    ModuleInfo.Load(Path.Combine("Build", "Module.xml")),
                    enabledServices: enabledServices.ToArray(),
                    disabledServices: disabledServices.ToArray(),
                    serviceSpecPath: serviceSpecPath);
        }

        private static void RunModuleManager()
        {
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
                using (var m = ResourceExtractor.GetTransparentDecompressionStream(Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Protobuild.BuildResources.ProtobuildManager.exe.gz")))
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
