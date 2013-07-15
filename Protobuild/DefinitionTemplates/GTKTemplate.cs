using System.IO;

namespace Protobuild
{
    public class GTKTemplate : BaseTemplate
    {
        public override string Type { get { return "GTK"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Project
  Name=""" + name + @"""
  Path=""" + name + @"""
  Type=""GTK"">
  <References>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""Microsoft.CSharp"" />
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
using Gtk;

namespace " + name + @"
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Application.Init();
            
            // Construct your main window here and call .Show() on it.
            
            Application.Run();
        }
    }
}");
            }
        }
        
        public override string GetIcon()
        {
            return "ProtobuildManager.Images.application.png";
        }
    }
}

