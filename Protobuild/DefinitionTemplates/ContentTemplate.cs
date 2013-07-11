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
        
        public override Gdk.Pixbuf GetIcon()
        {
            return new Gdk.Pixbuf(
                System.Reflection.Assembly.GetExecutingAssembly(),
                "Protobuild.Images.color_wheel.png");
        }
    }
}

