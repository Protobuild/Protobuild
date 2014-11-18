using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Protobuild
{
    public class FileFilterParser : IFileFilterParser
    {
        public void ParseAndApply(FileFilter result, Stream inputFilterFile, Dictionary<string, Action<FileFilter>> customDirectives)
        {
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
            using (var reader = new StreamReader(inputFilterFile))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (line.TrimStart().StartsWith("#") || line.Trim() == "")
                        continue;
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
                        default:
                            if (customDirectives.ContainsKey(mode))
                            {
                                customDirectives[mode](result);
                            }
                            break;
                    }
                }
            }
            return;
        }
    }
}
