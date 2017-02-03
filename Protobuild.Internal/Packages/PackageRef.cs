using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Protobuild
{
    /// <summary>
    /// A reference to a Protobuild package.
    /// </summary>
    public struct PackageRef
    {
        /// <summary>
        /// The unique URI to the package.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The Git reference or version number for the package.
        /// </summary>
        public string GitRef { get; set; }

        /// <summary>
        /// The folder that the package is extracted to under this module.
        /// </summary>
        public string Folder { get; set; }

        /// <summary>
        /// The platforms that this package is active for.
        /// </summary>
        public string[] Platforms { get; set; }

        /// <summary>
        /// The unique URI as a <see cref="Uri"/> object.
        /// </summary>
        public Uri UriObject
        {
            get
            {
                return new Uri(this.Uri);
            }
        }

        /// <summary>
        /// Whether or not the package reference is correctly formed.
        /// </summary>
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

        /// <summary>
        /// Whether or not the version reference is a commit hash or to a static, unchanging version number on e.g. NuGet.
        /// </summary>
        public bool IsStaticReference 
        {
            get
            {
                if (Uri != null && GitRef != null)
                {
                    if (Uri.StartsWith("https-nuget-v3://") || Uri.StartsWith("http-nuget-v3://"))
                    {
                        var semVerRegex = new Regex(
                            @"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-[a-z][0-9a-z-]*)?$",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
                        if (semVerRegex.IsMatch(this.GitRef))
                        {
                            return true;
                        }
                    }
                }

                if (GitRef != null)
                {
                    if (this.GitRef.Length != 40)
                    {
                        return false;
                    }

                    return System.Text.RegularExpressions.Regex.Match(this.GitRef, "^[0-9a-f]{40,40}$").Success;
                }

                return false;
            }
        }

        /// <summary>
        /// Whether or not this package reference is active for the target platform.
        /// </summary>
        /// <param name="platform">The target platform.</param>
        /// <returns>Whether or not this package reference is active for the target platform.</returns>
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

