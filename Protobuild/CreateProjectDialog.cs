using System;
using Gtk;

namespace Protobuild
{
    public partial class CreateProjectDialog : Gtk.Dialog
    {
        public CreateProjectDialog(Window window, string type, bool forceRoot = false) : base(
            "Create Project",
            window,
            Gtk.DialogFlags.Modal)
        {
            this.Build();
            
            if (type != "Module")
            {
                this.c_PromptLabel.Text = "Create " + type + " Project:";
                this.Title = "Create " + type + " Project";
            }
            else if (!forceRoot)
            {
                this.c_PromptLabel.Text = "Create Submodule:";
                this.Title = "Create Submodule";
            }
            else
            {
                this.c_PromptLabel.Text = "Create New Module:";
                this.Title = "Create New Module";
            }
        }
        
        public string ProjectName
        {
            get { return this.c_ProjectNameEntry.Text; }
        }
    }
}

