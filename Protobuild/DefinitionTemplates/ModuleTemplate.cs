using System;
using System.IO;
using System.Reflection;

namespace Protobuild
{
    public class ModuleTemplate : BaseTemplate
    {
        public override string Type { get { return "Module"; } }
        
        public override void WriteDefinitionFile(string name, Stream output)
        {
            // No definition file to write.
        }
        
        public override void CreateFiles(string name, string projectRoot)
        {
            var buildFolder = Path.Combine(projectRoot, "Build");
            if (!Directory.Exists(buildFolder))
                Directory.CreateDirectory(buildFolder);
            if (projectRoot != Environment.CurrentDirectory)
            {
                File.Copy(Assembly.GetExecutingAssembly().Location,
                    Path.Combine(projectRoot, "Protobuild.exe"));
            }
            ResourceExtractor.ExtractAll(buildFolder, name);
        }
        
        public override Gdk.Pixbuf GetIcon()
        {
            return new Gdk.Pixbuf(
                Assembly.GetExecutingAssembly(),
                "Protobuild.Images.box.png");
        }
    }
}

