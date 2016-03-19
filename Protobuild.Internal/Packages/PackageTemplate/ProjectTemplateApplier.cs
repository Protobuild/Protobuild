using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Protobuild
{
    internal class ProjectTemplateApplier : IProjectTemplateApplier
    {
        public void Apply(string stagingFolder, string templateName)
        {
            ApplyProjectTemplateFromStaging(stagingFolder, templateName, NormalizeTemplateName(templateName));
        }

        private void ApplyProjectTemplateFromStaging(string folder, string name, string normalizedTemplateName)
        {
            foreach (var pathToFile in GetFilesFromStaging(folder))
            {
                var path = pathToFile.Key;
                var file = pathToFile.Value;

                var replacedPath = path.Replace("{PROJECT_NAME}", name);
                replacedPath = replacedPath.Replace("{PROJECT_SAFE_NAME}", normalizedTemplateName);
                var dirSeperator = replacedPath.LastIndexOfAny(new[] {'/', '\\'});
                if (dirSeperator != -1)
                {
                    var replacedDir = replacedPath.Substring(0, dirSeperator);
                    if (!Directory.Exists(replacedDir))
                    {
                        Directory.CreateDirectory(replacedDir);
                    }
                }

                string contents;
                using (var reader = new StreamReader(file.FullName))
                {
                    contents = reader.ReadToEnd();
                }

                if (contents.Contains("{PROJECT_NAME}") || contents.Contains("{PROJECT_XML_NAME}") ||
                    contents.Contains("{PROJECT_SAFE_NAME}") || contents.Contains("{PROJECT_SAFE_XML_NAME}"))
                {
                    contents = contents.Replace("{PROJECT_NAME}", name);
                    contents = contents.Replace("{PROJECT_XML_NAME}", SecurityElement.Escape(name));
                    contents = contents.Replace("{PROJECT_SAFE_NAME}", normalizedTemplateName);
                    contents = contents.Replace("{PROJECT_SAFE_XML_NAME}",
                        SecurityElement.Escape(normalizedTemplateName));
                    using (var writer = new StreamWriter(replacedPath))
                    {
                        writer.Write(contents);
                    }
                }
                else
                {
                    // If we don't see {PROJECT_NAME} or {PROJECT_XML_NAME}, use a straight
                    // file copy so that we don't break binary files.
                    File.Copy(file.FullName, replacedPath, true);
                }
            }
        }

        private IEnumerable<KeyValuePair<string, FileInfo>> GetFilesFromStaging(string currentDirectory,
            string currentPrefix = null)
        {
            if (currentPrefix == null)
            {
                currentPrefix = string.Empty;
            }

            var dirInfo = new DirectoryInfo(currentDirectory);
            foreach (var subdir in dirInfo.GetDirectories("*"))
            {
                if (subdir.Name == ".git")
                {
                    continue;
                }

                if (subdir.Name == "_TemplateOnly")
                {
                    continue;
                }

                var nextDirectory = Path.Combine(currentDirectory, subdir.Name);
                var nextPrefix = currentPrefix == string.Empty ? subdir.Name : Path.Combine(currentPrefix, subdir.Name);

                foreach (var kv in GetFilesFromStaging(nextDirectory, nextPrefix))
                {
                    yield return kv;
                }
            }

            foreach (var file in dirInfo.GetFiles("*"))
            {
                yield return new KeyValuePair<string, FileInfo>(Path.Combine(currentPrefix, file.Name), file);
            }
        }

        private string NormalizeTemplateName(string name)
        {
            var normalized = string.Empty;
            for (var i = 0; i < name.Length; i++)
            {
                if ((name[i] >= 'a' && name[i] <= 'z') ||
                    (name[i] >= 'A' && name[i] <= 'Z') ||
                    (i >= 1 && name[i] >= '0' && name[i] <= '9'))
                {
                    normalized += name[i];
                }
            }
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "Default";
            }
            return normalized;
        }
    }
}