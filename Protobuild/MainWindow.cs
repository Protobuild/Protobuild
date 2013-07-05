using System.Linq;
using Gtk;
using Protobuild;
using Gdk;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System;

public partial class MainWindow: Gtk.Window
{
    private TreeStore m_Store;

    public ModuleInfo Module
    {
        get;
        set;
    }

    public MainWindow(): base (Gtk.WindowType.Toplevel)
    {
        Build();
        
        this.c_ProjectTreeView.AppendColumn("Icon", new CellRendererPixbuf(), "pixbuf", 0);
        this.c_ProjectTreeView.AppendColumn("Project", new CellRendererText(), "text", 1);
        
        this.m_Store = new TreeStore(typeof(Pixbuf), typeof(string), typeof(object));
        
        this.c_ProjectTreeView.Model = this.m_Store;
        this.c_ProjectTreeView.HeadersVisible = false;
        
        // Register events.
        this.c_CreateGameAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("XNA"); };
        this.c_CreateGUIAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("GUI"); };
        this.c_CreateLibraryAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("Library"); };
        this.c_CreateConsoleAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("Console"); };
        this.c_CreateWebsiteAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("Website"); };
        this.c_CreateTestsAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("Tests"); };
        this.c_CreateContentAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("Content"); };
        this.c_CreateModuleAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("Module"); };
        this.c_CreateExternalAction.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject("External"); };
        this.c_RegenerateAction.Activated += (object sender, EventArgs e) => 
        {
            this.RegenerateProjects();
        };
        this.c_ProjectTreeView.RowActivated += (object o, RowActivatedArgs args) => 
        {
            TreeIter iter;
            this.m_Store.GetIter(out iter, args.Path);
            var result = this.m_Store.GetValue(iter, 2);
            var definitionInfo = result as DefinitionInfo;
            var moduleInfo = result as ModuleInfo;
            if (definitionInfo != null)
            {
                // Open XML in editor.
                Process.Start("monodevelop", definitionInfo.DefinitionPath);
            }
            if (moduleInfo != null)
            {
                // Start the module's Protobuild unless it's also our
                // module (for the root node).
                if (moduleInfo.Path != this.Module.Path)
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = System.IO.Path.Combine(moduleInfo.Path, "Protobuild.exe"),
                        WorkingDirectory = moduleInfo.Path
                    };
                    var p = Process.Start(info);
                    p.EnableRaisingEvents = true;
                    p.Exited += (object sender, EventArgs e) => this.Update();
                }
            }
        };
    }
    
    public void Update()
    {
        this.BuildTree();
        this.RegenerateProjects();
    }
    
    private void RegenerateProjects()
    {
        var info = new ProcessStartInfo
        {
            FileName = "xbuild",
            Arguments = "Build" + System.IO.Path.DirectorySeparatorChar + "Main.proj /p:TargetPlatform=" + this.DetectPlatform(),
            WorkingDirectory = Environment.CurrentDirectory
        };
        Process.Start(info);
    }
    
    private string DetectPlatform()
    {
        if (System.IO.Path.DirectorySeparatorChar == '/')
        {
            if (Directory.Exists("/home"))
                return "Linux";
            return "MacOS";
        }
        return "Windows";
    }
    
    private void PromptCreateProject(string type)
    {
        var create = new CreateProjectDialog(this, type);
        create.Modal = true;
        if ((ResponseType)create.Run() == ResponseType.Ok)
        {
            string error;
            if (!this.CreateProject(create.ProjectName, type, out error))
            {
                var errorDialog = new MessageDialog(
                    this,
                    DialogFlags.Modal,
                    MessageType.Error,
                    ButtonsType.Ok,
                    "Unable to create project: " + error);
                errorDialog.Run();
                errorDialog.Destroy();
            }
            else
            {
                this.Update();
                if (type != "Module")
                    Process.Start("monodevelop", System.IO.Path.Combine("Build", "Projects", create.ProjectName + ".definition"));
                else
                {
                    var info = new ProcessStartInfo
                    {
                        FileName = System.IO.Path.Combine(create.ProjectName, "Protobuild.exe"),
                        WorkingDirectory = System.IO.Path.Combine(Environment.CurrentDirectory, create.ProjectName)
                    };
                    var p = Process.Start(info);
                    p.EnableRaisingEvents = true;
                    p.Exited += (object sender, EventArgs e) => this.Update();
                }
            }
        }
        create.Destroy();
    }
    
    public bool CreateProject(string name, string type, out string error, bool forceCurrent = false)
    {
        error = "No error";
        var template = BaseTemplate.GetTemplateForType(type);
        if (template == null)
        {
            error = "No template for this project type";
            return false;
        }
        if (!Regex.IsMatch(name, "^[A-Za-z\\.]+$"))
        {
            error = "Project name can only contain alphabetical characters and periods";
            return false;
        }
        if (!forceCurrent)
        {
            if (Directory.Exists(name))
            {
                error = "Directory " + name + " already exists";
                return false;
            }
            Directory.CreateDirectory(name);
        }
        var noDefinition = false;
        if (!Directory.Exists("Build"))
            Directory.CreateDirectory("Build");
        if (!Directory.Exists(System.IO.Path.Combine("Build", "Projects")))
            Directory.CreateDirectory(System.IO.Path.Combine("Build", "Projects"));
        using (var stream = new FileStream(
            System.IO.Path.Combine("Build", "Projects", name + ".definition"),
            FileMode.CreateNew))
        {
            template.WriteDefinitionFile(name, stream);
            try
            {
                if (stream.Position == 0)
                    noDefinition = true;
            }
            catch (ObjectDisposedException) { }
        }
        if (noDefinition)
            File.Delete(System.IO.Path.Combine("Build", "Projects", name + ".definition"));
        if (forceCurrent)
            template.CreateFiles(name, Environment.CurrentDirectory);
        else
            template.CreateFiles(name, System.IO.Path.Combine(
                Environment.CurrentDirectory,
                name));
        return true;
    }
    
    private Pixbuf GetPixbufForType(string type)
    {
        switch (type)
        {
            case "XNA":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.controller.png");
            case "GUI":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.application_osx.png");
            case "Console":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.application_osx_terminal.png");
            case "Library":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.bricks.png");
            case "Website":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.world.png");
            case "Tests":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.bug.png");
            case "Content":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.color_wheel.png");
            case "External":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.link_go.png");
            case "Module":
                return new Pixbuf(
                    Assembly.GetExecutingAssembly(),
                    "Protobuild.Images.box.png");
        }
        return null;
    }
    
    private void BuildTreeIter(TreeIter iter, ModuleInfo info)
    {
        foreach (var module in info.GetSubmodules().OrderBy(x => x.Name))
        {
            var subIter = this.m_Store.AppendValues(iter, GetPixbufForType("Module"), module.Name, module);
            this.BuildTreeIter(subIter, module);
        }
        
        foreach (var definition in info.GetDefinitions().OrderBy(x => x.Type).ThenBy(x => x.Name))
        {
            this.m_Store.AppendValues(iter, GetPixbufForType(definition.Type), definition.Name, definition);
        }
    }
    
    public void BuildTree()
    {
        // Update message.
        this.c_CreateMessageLabel.Text =
            "This instance of Protobuild will add " +
            "projects to '" + this.Module.Name + "'.  To add \r\n" +
            "projects to submodules, open Protobuild in the " +
            "root of those modules.";
    
        // Dumb rebuild.
        this.m_Store.Clear();
        
        var iter = this.m_Store.AppendValues(GetPixbufForType("Module"), this.Module.Name);
        this.BuildTreeIter(iter, this.Module);
        
        this.c_ProjectTreeView.ExpandAll();
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }
}
