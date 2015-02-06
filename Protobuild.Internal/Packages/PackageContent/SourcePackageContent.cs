﻿using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Protobuild
{
    public class SourcePackageContent : IPackageContent
    {
        private readonly IPackageCache m_PackageCache;

        public SourcePackageContent(IPackageCache packageCache)
        {
            this.m_PackageCache = packageCache;
        }

        public string OriginalGitUri { get; set; }

        public string SourcePath { get; set; }

        public string GitRef { get; set; }

        public void ExtractTo(string path)
        {
            // FIXME: This assumes packages are being extracted underneath the current
            // working directory (i.e. the module root).
            if (GitUtils.IsGitRepository())
            {
                GitUtils.UnmarkIgnored(path);
                GitUtils.RunGit(null, "submodule update --init --recursive");

                if (!File.Exists(Path.Combine(path, ".git")))
                {
                    // The submodule has never been added.
                    GitUtils.RunGit(null, "submodule add --reference " + this.SourcePath + " " + this.OriginalGitUri + " " + path);
                    GitUtils.RunGit(path, "checkout -f " + this.GitRef);
                    this.InitializeSubmodulesFromCache(null);
                    GitUtils.RunGit(null, "add .gitmodules");
                    GitUtils.RunGit(null, "add " + path);
                }

                GitUtils.MarkIgnored(path);
            }
            else
            {
                // The current folder isn't a Git repository, so use
                // git clone instead of git submodule.
                GitUtils.RunGit(null, "clone " + this.SourcePath + " " + path);
                GitUtils.RunGit(path, "checkout -f " + this.GitRef);
                this.InitializeSubmodulesFromCache(path);
            }
        }

        private void InitializeSubmodulesFromCache(string path)
        {
            GitUtils.RunGit(path, "submodule init");
            var submodules = GitUtils.RunGitAndCapture(path, "config --local --list");
            foreach (Match match in new Regex(@"submodule\.(?<name>.*)\.url=(?<url>.*)").Matches(submodules))
            {
                var name = match.Groups["name"].Value;
                var url = match.Groups["url"].Value;

                var submodule = (SourcePackageContent)this.m_PackageCache.GetSourcePackage(url, "");
                GitUtils.RunGit(path, "config --local submodule." + name + ".url " + submodule.SourcePath);
                GitUtils.RunGit(path, "submodule update " + name);
                this.InitializeSubmodulesFromCache(Path.Combine(path ?? "", name));
                GitUtils.RunGit(path, "config --local submodule." + name + ".url " + url);
            }
        }
    }
}

