using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Gdk;
using Gtk;
using Protobuild;

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
        this.c_RegenerateAction.Activated += (sender, e) => Actions.PerformAction(this.Module, "resync");
        this.c_ProjectTreeView.RowActivated += (o, args) => 
        {
            TreeIter iter;
            this.m_Store.GetIter(out iter, args.Path);
            var result = this.m_Store.GetValue(iter, 2);
            Actions.Open(this.Module, result, this.Update);
        };
    }
    
    public void InitializeToolbar()
    {
        this.LoadBuiltinTemplates();
        this.LoadModuleTemplates(this.Module);
    }
    
    private void AddToToolbar(BaseTemplate i)
    {
        var w1 = new IconFactory();
        var w2 = new IconSet(new Pixbuf(
            Assembly.GetExecutingAssembly(),
            i.GetIcon()));
        w1.Add(i.Type + "_icon", w2);
        w1.AddDefault();
        var act = new Gtk.Action("c_Create" + i.Type + "Action", null, "Create " + i.Type + " Project", null);
        act.Activated +=
            (object sender, System.EventArgs e) =>
                { this.PromptCreateProject(i.Type); };
        var t = act.CreateToolItem();
        (t as ToolButton).StockId = i.Type + "_icon";
        this.c_Toolbar.Add(t);
    }
    
    private void LoadBuiltinTemplates()
    {
        foreach (var i in from assembly in AppDomain.CurrentDomain.GetAssemblies()
                          where assembly.GetName().Name.StartsWith("Protobuild")
                          from concreteType in assembly.GetTypes()
                          where !concreteType.IsAbstract
                          where typeof(BaseTemplate).IsAssignableFrom(concreteType)
                          let i = Activator.CreateInstance(concreteType) as BaseTemplate
                          select i)
        {
            this.AddToToolbar(i);
        }
    }
    
    private void LoadModuleTemplates(ModuleInfo module)
    {
        foreach (var i in module.GetTemplates())
        {
            this.AddToToolbar(i);
        }
        foreach (var submodule in module.GetSubmodules())
            this.LoadModuleTemplates(submodule);
    }
    
    public void Update()
    {
        this.BuildTree();
        Actions.PerformAction(this.Module, "resync");
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
        var template = BaseTemplate.GetTemplateForType(type);
        return new Pixbuf(
            Assembly.GetExecutingAssembly(),
            template.GetIcon() ?? "ProtobuildManager.Images.bricks.png");
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
