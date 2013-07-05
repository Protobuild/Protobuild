using System;
using System.IO;

namespace Protobuild
{
    public class LibraryTemplate : BaseTemplate
    {
        public override string Type { get { return "Library"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Project
  Name=""" + name + @"""
  Path=""" + name + @"""
  Type=""Library"">

  <!--
    Your project files are generated from these definition files.  When
    adding, renaming or deleting files in your project, or when you want
    to change the references in a project, you need to do it in the 
    definitions file.  Changes to the project via MonoDevelop will be lost
    the next time the projects are regenerated.
    
    By generating the solution and project files from these definitions,
    it greatly reduces the number of cross-platform issues and benefits
    testing and repeatability of builds.
  -->
  
  <References>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""Microsoft.CSharp"" />
  </References>
  <Files>
    <Compile Include=""MyClass.cs"" />
  </Files>
</Project>");
            }
        }
        
        public override void CreateFiles(string name, string projectRoot)
        {
            using (var writer = new StreamWriter(Path.Combine(projectRoot, "MyClass.cs")))
            {
                writer.Write(
@"using System;

namespace " + name + @"
{
    public class MyClass
    {
    }
}");
            }
        }
    }
}

