using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;

namespace Protobuild
{
    using System.Collections.Generic;
    using System.IO.Compression;

    class MainClass
    {
        public static void Main(string[] args)
        {
            var needToExit = false;
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
                                    .GetManifestResourceStream("Protobuild.BuildResources.GenerateProject.xslt.lzma"))
                                .CopyTo(writer.BaseStream);
                            writer.Flush();
                        }
                        using (var writer = new StreamWriter(Path.Combine("Build", "GenerateSolution.xslt")))
                        {
                            ResourceExtractor.GetTransparentDecompressionStream(
                                Assembly.GetExecutingAssembly()
                                    .GetManifestResourceStream("Protobuild.BuildResources.GenerateSolution.xslt.lzma"))
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
            Action<string[]> helpAction = x =>
            {
                Console.WriteLine("Protobuild.exe [options]");
                Console.WriteLine();
                Console.WriteLine("By default Protobuild resynchronises or generates projects for");
                Console.WriteLine("the current platform, depending on the module configuration.");
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
                Directory.CreateDirectory("Build");
                ResourceExtractor.ExtractAll(Path.Combine(Environment.CurrentDirectory, "Build"), "MyProject");
                Console.WriteLine("Build" + Path.DirectorySeparatorChar + "Module.xml has been created.");
                Environment.Exit(0);
            }

            Actions.DefaultAction(
                ModuleInfo.Load(Path.Combine("Build", "Module.xml")),
                enabledServices: enabledServices.ToArray(),
                disabledServices: disabledServices.ToArray(),
                serviceSpecPath: serviceSpecPath);
        }
    }
}
