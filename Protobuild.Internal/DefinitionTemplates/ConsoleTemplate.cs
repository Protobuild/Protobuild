using System.IO;

namespace Protobuild
{
    public class ConsoleTemplate : BaseTemplate
    {
        public override string Type { get { return "Console"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Project
  Name=""" + name + @"""
  Path=""" + name + @"""
  Type=""Console"">
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

namespace " + name + @"
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // TODO: Implement console app.
        }
    }
}");
            }
        }
        
        public override string GetIcon()
        {
            return "ProtobuildManager.Images.application_osx_terminal.png";
        }
    }
}

