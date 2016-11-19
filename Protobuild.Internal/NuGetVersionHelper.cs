using System.Security.Cryptography;
using System.Text;

namespace Protobuild.Internal
{
    public static class NuGetVersionHelper
    {
        public static string CreateNuGetPackageVersion(string gitHash, string platform)
        {
            // NuGet has a maximum of 64 characters for a version string, so we must hash Git + platform
            // so that it's short enough to fit.
            using (var sha1 = new SHA1Managed())
            {
                var verComponent = "GIT" + gitHash + "-PLATFORM" + platform;
                var hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(verComponent));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return "0.0.0-SHA" + sb;
            }
        }
    }
}
