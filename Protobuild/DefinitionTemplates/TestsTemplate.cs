using System;
using System.IO;

namespace Protobuild
{
    public class TestsTemplate : BaseTemplate
    {
        public override string Type { get { return "Tests"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<Project
  Name=""" + name + @"""
  Path=""" + name + @"""
  Type=""Tests"">

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
    <Compile Include=""ClassTests.cs"" />
  </Files>
</Project>");
            }
        }
        
        public override void CreateFiles(string name, string projectRoot)
        {
            using (var writer = new StreamWriter(Path.Combine(projectRoot, "ClassTests.cs")))
            {
                writer.Write(
@"using System;
using Xunit;

//
// Use the NuGet package manager to reference Xunit for this
// project.  You can do this from MonoDevelop by right-clicking
// the project and going ""Manage NuGet Packages..."" and
// searching for ""Xunit"".
//
// If you don't see the NuGet dropdown, you might not have the
// add-on installed.  See https://github.com/mrward/monodevelop-nuget-addin
// for instructions on how to install the NuGet plugin for
// MonoDevelop.
//

namespace " + name + @"
{
    public class ClassTests
    {
        [Fact]
        public void TestTrue()
        {
            Assert.Equal(true, true);
        }
    }
}");
            }
        }
    }
}

