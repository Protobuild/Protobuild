using System.IO;

namespace Protobuild
{
    public class GUITemplate : BaseTemplate
    {
        public override string Type { get { return "GUI"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Project
  Name=""" + name + @"""
  Path=""" + name + @"""
  Type=""GUI"">
  <References>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""Microsoft.CSharp"" />
    <Reference Include=""System.Windows.Forms"" />
  </References>
  <Files>
    <Compile Include=""Program.cs"" />
  </Files>
</Project>");
            }
        }
        
        public override void CreateFiles(string name, string projectRoot)
        {
            using (var writer = new StreamWriter(Path.Combine(projectRoot, "Program.cs")))
            {
                writer.Write(
@"using System;

namespace " + name + @"
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // TODO: Implement graphical app.
        }
    }
}");
            }
        }
        
        public override Gdk.Pixbuf GetIcon()
        {
            return new Gdk.Pixbuf(
                System.Reflection.Assembly.GetExecutingAssembly(),
                "Protobuild.Images.application_osx.png");
        }
    }
}

