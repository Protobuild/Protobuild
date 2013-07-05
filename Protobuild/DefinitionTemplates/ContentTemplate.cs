using System;
using System.IO;

namespace Protobuild
{
    public class ContentTemplate : BaseTemplate
    {
        public override string Type { get { return "Content"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<ContentProject Name=""" + name + @""">

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
  
  <!--
    List the folders relative to the module root that you want
    to search for files in.  This is a recursive search for the
    specified files, which then get included in projects that
    reference '" + name + @"' as copy-on-build.
    
    Example:
    
    <Source Include=""" + name + @"/compiled/Content"" Match=""*.xnb"" />
    <Source Include=""" + name + @"/assets"" Match=""*.asset"" />
  -->
  
</ContentProject>");
            }
        }
        
        public override void CreateFiles(string name, string projectRoot)
        {
        }
    }
}

