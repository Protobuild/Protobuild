using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Protobuild
{
    internal class FileFilter : IEnumerable<KeyValuePair<string, List<string>>>
    {
        private List<string> m_SourceFiles = new List<string>();
        private Dictionary<string, List<string>> m_FileMappings = new Dictionary<string, List<string>>();
        private int? _targetPathCount;

        public FileFilter(IEnumerable<string> filenames)
        {
            foreach (string s in filenames)
                this.m_SourceFiles.Add(s);
        }

        public List<string> FindInSourceFiles(string searchRegex)
        {
            var uniqueResults = new List<string>();
            var re = new Regex(searchRegex);

            foreach (var src in this.m_SourceFiles)
            {
                if (re.IsMatch(src))
                {
                    if (!uniqueResults.Contains(src))
                    {
                        uniqueResults.Add(src);
                    }
                }
            }

            return uniqueResults;
        }

        public void AddManualMapping(string source, string destination)
        {
            this.m_FileMappings.Add(source, new List<string> {destination});
        }

        public bool ApplyInclude(string regex)
        {
            var didMatch = false;
            var re = new Regex(regex);
            foreach (string s in this.m_SourceFiles)
            {
                if (re.IsMatch(s))
                {
                    if (!this.m_FileMappings.ContainsKey(s))
                    {
                        this.m_FileMappings.Add(s, new List<string> {s});
                    }
                    else
                    {
                        this.m_FileMappings[s].Add(s);
                    }

                    didMatch = true;
                }
            }
            _targetPathCount = null;
            return didMatch;
        }

        public bool ApplyExclude(string regex)
        {
            var didMatch = false;
            var re = new Regex(regex);
            var toRemove = new List<string>();
            foreach (KeyValuePair<string, List<string>> kv in this.m_FileMappings)
            {
                foreach (var v in kv.Value.ToArray())
                {
                    if (re.IsMatch(v))
                    {
                        kv.Value.Remove(v);
                        didMatch = true;
                    }

                    if (kv.Value.Count == 0)
                    {
                        // no mappings left
                        toRemove.Add(kv.Key);
                    }
                }
            }
            foreach (string s in toRemove)
            {
                this.m_FileMappings.Remove(s);
            }
            _targetPathCount = null;
            return didMatch;
        }

        public bool ApplyRewrite(string find, string replace)
        {
            var didMatch = false;
            var re = new Regex(find);
            var copy = new Dictionary<string, List<string>>(this.m_FileMappings);
            foreach (KeyValuePair<string, List<string>> kv in copy)
            {
                var a = kv.Value.ToArray();
                for (var i = 0; i < a.Length; i++)
                {
                    if (re.IsMatch(a[i]))
                    {
                        this.m_FileMappings[kv.Key][i] = re.Replace(a[i], replace);
                        didMatch = true;
                    }
                }
            }
            _targetPathCount = null;
            return didMatch;
        }

        public bool ApplyCopy(string find, string target)
        {
            var didMatch = false;
            var re = new Regex(find);
            var copy = new Dictionary<string, List<string>>(this.m_FileMappings);
            foreach (KeyValuePair<string, List<string>> kv in copy)
            {
                var a = kv.Value.ToArray();
                for (var i = 0; i < a.Length; i++)
                {
                    if (re.IsMatch(a[i]))
                    {
                        this.m_FileMappings[kv.Key].Add(re.Replace(a[i], target));
                        didMatch = true;
                    }
                }
            }
            _targetPathCount = null;
            return didMatch;
        }

        public void ImplyDirectories()
        {
            var directoriesNeeded = new HashSet<string>();

            foreach (var mappingCopy in this.m_FileMappings)
            {
                foreach (var filename in mappingCopy.Value)
                {
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
            }

            foreach (var dir in directoriesNeeded)
            {
                this.m_FileMappings.Add(dir, new List<string> { dir + "/" });
            }

            _targetPathCount = null;
        }

        public bool ContainsTargetPath(string path)
        {
            foreach (var mapping in this.m_FileMappings)
            {
                foreach (var entry in mapping.Value)
                {
                    if (entry == path)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public int CountTargetPaths()
        {
            if (_targetPathCount != null)
            {
                return _targetPathCount.Value;
            }

            var i = 0;
            foreach (var mapping in this.m_FileMappings)
            {
                i += mapping.Value.Count;
            }

            _targetPathCount = i;
            return i;
        }

        public IEnumerable<KeyValuePair<string, string>> GetExpandedEntries()
        {
            foreach (var mapping in m_FileMappings)
            {
                foreach (var value in mapping.Value)
                {
                    yield return new KeyValuePair<string, string>(mapping.Key, value);
                }
            }
        }

        #region IEnumerable<KeyValuePair<string,string>> Members

        public IEnumerator<KeyValuePair<string, List<string>>> GetEnumerator()
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
