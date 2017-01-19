using System;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Protobuild
{
    internal class ModuleExecution : IModuleExecution
    {
        private static object _selfWorkingDirectoryLock = new object();

        public Tuple<int, string, string> RunProtobuild(ModuleInfo module, string args, bool capture = false)
        {
            var invokeInline = false;
            var myHash = ExecEnvironment.GetProgramHash();
            var targetHash = ExecEnvironment.GetProgramHash(Path.Combine(module.Path, "Protobuild.exe"));
            if (myHash == targetHash)
            {
                invokeInline = true;
            }

            if (ExecEnvironment.RunProtobuildInProcess || invokeInline)
            {
                var oldBuffer = RedirectableConsole.TargetBuffer;
                var ourBuffer = new RedirectableBuffer();
                RedirectableConsole.TargetBuffer = ourBuffer;
                var needsEndSelfInvoke = true;
                lock (_selfWorkingDirectoryLock)
                {
                    var oldLock = _selfWorkingDirectoryLock;
                    _selfWorkingDirectoryLock = new object();
                    var old = Environment.CurrentDirectory;
                    try
                    {
                        Environment.CurrentDirectory = module.Path;
                        var exitCode = ExecEnvironment.InvokeSelf(args.SplitCommandLine().ToArray());
                        RedirectableConsole.TargetBuffer = oldBuffer;
                        needsEndSelfInvoke = false;
                        if (capture)
                        {
                            return new Tuple<int, string, string>(
                                exitCode,
                                ourBuffer.Stdout,
                                ourBuffer.Stderr);
                        }
                        else
                        {
                            return new Tuple<int, string, string>(
                                exitCode,
                                string.Empty,
                                string.Empty);
                        }
                    }
                    finally
                    {
                        if (needsEndSelfInvoke)
                        {
                            RedirectableConsole.TargetBuffer = oldBuffer;
                        }
                        Environment.CurrentDirectory = old;
                        _selfWorkingDirectoryLock = oldLock;
                    }
                }
            }

            var protobuildPath = Path.Combine(module.Path, "Protobuild.exe");

            try
            {
                var chmodStartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = "a+x Protobuild.exe",
                    WorkingDirectory = module.Path,
                    CreateNoWindow = capture,
                    UseShellExecute = false
                };
                Process.Start(chmodStartInfo);
            }
            catch (ExecEnvironment.SelfInvokeExitException)
            {
                throw;
            }
            catch
            {
            }

            var stdout = string.Empty;
            var stderr = string.Empty;

            for (var attempt = 0; attempt < 3; attempt++)
            {
                if (File.Exists(protobuildPath))
                {
                    var pi = new ProcessStartInfo
                    {
                        FileName = protobuildPath,
                        Arguments = args,
                        WorkingDirectory = module.Path,
                        CreateNoWindow = capture,
                        RedirectStandardError = capture,
                        RedirectStandardInput = capture,
                        RedirectStandardOutput = capture,
                        UseShellExecute = false
                    };
                    var p = new Process { StartInfo = pi };
                    if (capture)
                    {
                        p.OutputDataReceived += (sender, eventArgs) =>
                        {
                            if (!string.IsNullOrEmpty(eventArgs.Data))
                            {
                                if (capture)
                                {
                                    stdout += eventArgs.Data + "\n";
                                }
                                else
                                {
                                    RedirectableConsole.WriteLine(eventArgs.Data);
                                }
                            }
                        };
                        p.ErrorDataReceived += (sender, eventArgs) =>
                        {
                            if (!string.IsNullOrEmpty(eventArgs.Data))
                            {
                                if (capture)
                                {
                                    stderr += eventArgs.Data + "\n";
                                }
                                else
                                {
                                    RedirectableConsole.ErrorWriteLine(eventArgs.Data);
                                }
                            }
                        };
                    }
                    try
                    {
                        p.Start();
                    }
                    catch (System.ComponentModel.Win32Exception ex)
                    {
                        if (ex.Message.Contains("Cannot find the specified file"))
                        {
                            // Mono sometimes throws this error even though the
                            // file does exist on disk.  The best guess is there's
                            // a race condition between performing chmod on the
                            // file and Mono actually seeing it as an executable file.
                            // Show a warning and sleep for a bit before retrying.
                            if (attempt != 2)
                            {
                                RedirectableConsole.WriteLine("WARNING: Unable to execute Protobuild.exe, will retry again...");
                                System.Threading.Thread.Sleep(2000);
                                continue;
                            }
                            else
                            {
                                RedirectableConsole.WriteLine("ERROR: Still unable to execute Protobuild.exe.");
                                throw;
                            }
                        }
                    }
                    if (capture)
                    {
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                    }
                    p.WaitForExit();
                    return new Tuple<int, string, string>(p.ExitCode, stdout, stderr);
                }
            }

            return new Tuple<int, string, string>(1, string.Empty, string.Empty);
        }
    }
}

