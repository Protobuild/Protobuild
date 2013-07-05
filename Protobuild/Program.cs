using System;
using Gtk;
using System.IO;

namespace Protobuild
{
    class MainClass
    {
        public static void Main(string[] args)
        {
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
            win.Update();
            
            win.Show();
            Application.Run();
        }
    }
}
