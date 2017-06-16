using System.IO;
using System.Diagnostics;

namespace Protobuild
{
    public class GraphicalAppDetection : IGraphicalAppDetection
    {
        public bool TargetMacOSAppIsGraphical(string appFolderPath)
        {
            var plistPath = Path.GetFullPath(Path.Combine(appFolderPath, "Contents/Info"));

            var startInfo = new ProcessStartInfo(
                "/usr/bin/defaults",
                "read \"" + plistPath + "\" NSPrincipalClass");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            var process = Process.Start(startInfo);
            if (process == null)
            {
                // Unable to run defaults; assume non-native.
                return false;
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            if (process.ExitCode == 0)
            {
                // NSPrincipalClass is present; this is a graphical app.
                return true;
            }

            return false;
        }
    }
}
