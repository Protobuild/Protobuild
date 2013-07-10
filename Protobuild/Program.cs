using System;
using Gtk;
using System.IO;
using System.Reflection;

namespace Protobuild
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var needToExit = false;
            var options = new Options();
            options["extract-xslt"] = () =>
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
            options["extract-proj"] = () =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    ResourceExtractor.ExtractProject(Path.Combine(Environment.CurrentDirectory, "Build"), module.Name);
                    needToExit = true;
                }
            };
            options["sync"] = () =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    Actions.Sync(module);
                    needToExit = true;
                }
            };
            options["resync"] = () =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    Actions.Resync(module);
                    needToExit = true;
                }
            };
            options["generate"] = () =>
            {
                if (Directory.Exists("Build"))
                {
                    var module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
                    Actions.RegenerateProjects(module.Path);
                    needToExit = true;
                }
            };
            options["help"] = () =>
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
                Console.WriteLine("  -generate");
                Console.WriteLine();
                Console.WriteLine("  Generates the C# project files from the definitions.");
                Console.WriteLine();
                needToExit = true;
            };
            options.Parse(args);
            if (needToExit)
            {
                Environment.Exit(0);
                return;
            }
        
            Application.Init();
            
            MainWindow win = new MainWindow();
                  
            // Detect if there is a Build directory in the local folder.  If not,
            // prompt to create a new module.
            if (!Directory.Exists("Build"))
            {
                var confirm = new MessageDialog(
                    win,
                    DialogFlags.Modal,
                    MessageType.Question,
                    ButtonsType.YesNo,
                    false,
                    "The current directory is not a Protobuild module.  Would " +
                    "you like to turn this directory into a module?");
                var result = (ResponseType)confirm.Run();
                if (result == ResponseType.No)
                {
                    // We can't run the module manager if the current directory
                    // isn't actually a module!
                    confirm.Destroy();
                    win.Destroy();
                    return;
                }
                confirm.Destroy();
                
                var create = new CreateProjectDialog(win, "Module", true);
                create.Modal = true;
                if ((ResponseType)create.Run() != ResponseType.Ok)
                {
                    create.Destroy();
                    win.Destroy();
                    return;
                }
                string error;
                if (!win.CreateProject(create.ProjectName, "Module", out error, true))
                {
                    var errorDialog = new MessageDialog(
                        win,
                        DialogFlags.Modal,
                        MessageType.Error,
                        ButtonsType.Ok,
                        "Unable to create module: " + error);
                    errorDialog.Run();
                    errorDialog.Destroy();
                    create.Destroy();
                    win.Destroy();
                    return;
                }
                Directory.CreateDirectory("Build");
                ResourceExtractor.ExtractAll(Path.Combine(
                    Environment.CurrentDirectory,
                    "Build"), create.ProjectName);
                create.Destroy();
            }
            
            // Load the module.
            win.Module = ModuleInfo.Load(Path.Combine("Build", "Module.xml"));
            win.InitializeToolbar();
            win.Update();
            
            win.Show();
            Application.Run();
        }
    }
}
