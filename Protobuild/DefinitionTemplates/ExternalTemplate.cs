using System.IO;

namespace Protobuild
{
    public class ExternalTemplate : BaseTemplate
    {
        public override string Type { get { return "External"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                writer.Write(
@"<?xml version=""1.0"" encoding=""utf-8"" ?>
<ExternalProject Name=""" + name + @""">
  
  <!--
  
    External projects allow you to refer to existing C# projects
    or precompiled binaries, optionally for specific platforms.
    
    To make a project or binary platform specific, nest the 
    Project or Binary elements inside a <Platform Type="""">
    element.
    
    === PLATFORM SPECIFIC C# PROJECTS ===
    
    <Platform Type=""Windows"">
      <Project 
        Name=""MonoGame.Framework.WindowsGL""
        Guid=""7DE47032-A904-4C29-BD22-2D235E8D91BA""
        Path=""Libraries/MonoGame/MonoGame.Framework/MonoGame.Framework.WindowsGL.csproj"">
      </Project>
      <Project
        Name=""Lidgren.Network.Windows""
        Guid=""AE483C29-042E-4226-BA52-D247CE7676DA""
        Path=""Libraries/MonoGame/ThirdParty/Lidgren.Network/Lidgren.Network.Windows.csproj"" />
    </Platform>
  
    === PLATFORM INDEPENDENT C# PROJECTS ===
    
    <Project 
      Name=""SomeLibrary""
      Guid=""12345678-ABCD-ABCD-ABCD-1234567890ABCD""
      Path=""Folder/Project.csproj"">
    </Project>
   
    === PLATFORM SPECIFIC BINARIES ===
    
    <Platform Type=""Windows"">
      <Binary
        Name=""ICSharp.Decompiler"" 
        Path=""Libraries/ICSharpCode/ICSharpCode.Decompiler.dll"" />
    </Platform>
  
    === PLATFORM INDEPENDENT BINARIES ===
    
    <Binary
      Name=""ICSharp.Decompiler"" 
      Path=""Libraries/ICSharpCode/ICSharpCode.Decompiler.dll"" />
      
  -->
  
</ExternalProject>");
            }
        }
        
        public override void CreateFiles(string name, string projectRoot)
        {
        }
        
        public override Gdk.Pixbuf GetIcon()
        {
            return new Gdk.Pixbuf(
                System.Reflection.Assembly.GetExecutingAssembly(),
                "Protobuild.Images.link_go.png");
        }
    }
}

