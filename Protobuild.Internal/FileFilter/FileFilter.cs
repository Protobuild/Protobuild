using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Protobuild
{
    public class FileFilter : IEnumerable<KeyValuePair<string, string>>
    {
        private List<string> m_SourceFiles = new List<string>();
        private Dictionary<string, string> m_FileMappings = new Dictionary<string, string>();

        public FileFilter(IEnumerable<string> filenames)
        {
            foreach (string s in filenames)
                this.m_SourceFiles.Add(s);
        }

        public void ApplyInclude(string regex)
        {
            var re = new Regex(regex);
            foreach (string s in this.m_SourceFiles)
                if (re.IsMatch(s))
                    this.m_FileMappings.Add(s, s);
        }

        public void ApplyExclude(string regex)
        {
            var re = new Regex(regex);
            var toRemove = new List<string>();
            foreach (KeyValuePair<string, string> kv in this.m_FileMappings)
                if (re.IsMatch(kv.Value))
                    toRemove.Add(kv.Key);
            foreach (string s in toRemove)
                this.m_FileMappings.Remove(s);
        }

        public void ApplyRewrite(string find, string replace)
        {
            var re = new Regex(find);
            var copy = new Dictionary<string, string>(this.m_FileMappings);
            foreach (KeyValuePair<string, string> kv in copy)
                if (re.IsMatch(kv.Value))
                    this.m_FileMappings[kv.Key] = re.Replace(kv.Value, replace);
        }

        public void ImplyDirectories()
        {
            var directoriesNeeded = new HashSet<string>();

            foreach (var mappingCopy in this.m_FileMappings)
            {
                var filename = mappingCopy.Value;
                var components = filename.Split('/', '\\');
                var stack = new List<string>();

                for (var i = 0; i < components.Length - 1; i++)
                {
                    stack.Add(components[i]);
                    if (!directoriesNeeded.Contains(string.Join("/", stack)))
                    {
                        directoriesNeeded.Add(string.Join("/", stack));
                    }
                }
            }

            foreach (var dir in directoriesNeeded)
            {
                this.m_FileMappings.Add(dir, dir + "/");
            }
        }

        #region IEnumerable<KeyValuePair<string,string>> Members

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return this.m_FileMappings.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)this.m_FileMappings).GetEnumerator();
        }

        #endregion

    }
}
