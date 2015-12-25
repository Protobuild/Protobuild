using System;
using System.Linq;

namespace Protobuild
{
    public struct PackageRef
    {
        public string Uri { get; set; }

        public string GitRef { get; set; }

        public string Folder { get; set; }

        public string[] Platforms { get; set; }

        public Uri UriObject
        {
            get
            {
                return new Uri(this.Uri);
            }
        }

        public bool Valid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.Uri))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(this.Folder))
                {
                    return false;
                }

                if (this.Folder.Contains("/") || this.Folder.Contains("\\"))
                {
                    return false;
                }

                var scheme = this.UriObject.Scheme.ToLowerInvariant();

                if (scheme != "http" && scheme != "https")
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsCommitReference 
        {
            get
            {
                if (this.GitRef.Length != 40)
                {
                    return false;
                }

                return System.Text.RegularExpressions.Regex.Match(this.GitRef, "^[0-9a-f]{40,40}$").Success;
            }
        }

        public bool IsActiveForPlatform(string platform)
        {
            if (Platforms == null || Platforms.Length == 0)
            {
                return true;
            }

            return Platforms.Select(x => x.ToLowerInvariant()).Contains(platform.ToLowerInvariant());
        }
    }
}

