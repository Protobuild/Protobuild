using System.IO;

namespace Protobuild
{
    public class XNATemplate : BaseTemplate
    {
        public override string Type { get { return "XNA"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Project
  Name=""" + name + @"""
  Path=""" + name + @"""
  Type=""App"">
  <References>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""Microsoft.CSharp"" />
    <Reference Include=""MonoGame.Framework"" />
    <Reference Include=""Ninject"" />
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
            // TODO: Implement game.
        }
    }
}");
            }
        }
        
        public override string GetIcon()
        {
            return "ProtobuildManager.Images.controller.png";
        }
    }
}

