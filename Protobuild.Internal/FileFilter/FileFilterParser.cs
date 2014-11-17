using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Protobuild
{
    public class FileFilterParser : IFileFilterParser
    {
        private readonly IAutomaticProjectPackager m_AutomaticProjectPackager;

        public FileFilterParser(IAutomaticProjectPackager automaticProjectPackager)
        {
            this.m_AutomaticProjectPackager = automaticProjectPackager;
        }

        public FileFilter Parse(ModuleInfo rootModule, string platform, string path, IEnumerable<string> filenames)
        {
            var result = new FileFilter(
                this.m_AutomaticProjectPackager,
                rootModule,
                platform,
                filenames);
            var isSlashed = false;
            Action init = () =>
            {
                isSlashed = false;
            };
            Func<char, bool> splitter = c =>
            {
                if (c == '\\')
                {
                    isSlashed = true;
                    return false;
                }

                if (c == ' ' && !isSlashed)
                    return true;

                isSlashed = false;
                return false;
            };
            using (var reader = new StreamReader(path))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line.TrimStart().StartsWith("#") || line.Trim() == "")
                        continue;
                    line = line.Replace("%PLATFORM%", platform);
                    var mode = line.Split(splitter, 2).ToStringArray()[0];
                    switch (mode)
                    {
                        case "include":
                            result.ApplyInclude(line.Init(init).Split(splitter, 2).ToStringArray()[1]);
                            break;
                        case "exclude":
                            result.ApplyExclude(line.Init(init).Split(splitter, 2).ToStringArray()[1]);
                            break;
                        case "rewrite":
                            result.ApplyRewrite(line.Init(init).Split(splitter, 3).ToStringArray()[1], line.Split(splitter, 3).ToStringArray()[2]);
                            break;
                        case "autoproject":
                            result.ApplyAutoProject();
                            break;
                    }
                }
            }
            return result;
        }
    }
}
