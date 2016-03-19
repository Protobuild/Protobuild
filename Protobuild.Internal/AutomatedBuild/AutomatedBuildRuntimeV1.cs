using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Protobuild
{
    internal class AutomatedBuildRuntimeV1 : IAutomatedBuildRuntimeV1
    {
        private readonly IHostPlatformDetector _hostPlatformDetector;
        private readonly IWorkingDirectoryProvider _workingDirectoryProvider;

        public AutomatedBuildRuntimeV1(IHostPlatformDetector hostPlatformDetector,
            IWorkingDirectoryProvider workingDirectoryProvider)
        {
            _hostPlatformDetector = hostPlatformDetector;
            _workingDirectoryProvider = workingDirectoryProvider;
        }

        public object Parse(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');

            var hostPlatform = _hostPlatformDetector.DetectPlatform();

            var instructions = new List<ParsedInstruction>();
            var currentLine = 1;
            foreach (var line in lines)
            {
                currentLine++;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (line.TrimStart().StartsWith("#"))
                {
                    continue;
                }

                try
                {
                    var instruction = line.Trim().Split(new[] {' '}, 2);
                    switch (instruction[0].ToLower())
                    {
                        case "if":
                        {
                            if (instruction.Length == 1)
                            {
                                throw new ParserErrorException(
                                    "Expected if condition, if line missing entirely",
                                    currentLine);
                            }

                            var components = instruction[1].Split(new[] { ' ' }, 2);
                            switch (components[0])
                            {
                                case "host":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Predicate = () => hostPlatform == components[1]
                                    });
                                    break;
                                case "host-not":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Predicate = () => hostPlatform != components[1]
                                    });
                                    break;
                                case "host-in":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Predicate = () => components[1].Split(',').Contains(hostPlatform)
                                    });
                                    break;
                                case "host-not-in":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Predicate = () => !components[1].Split(',').Contains(hostPlatform)
                                    });
                                break;
                                case "file-exists":
                                    instructions.Add(new ParsedInstruction
                                        {
                                            Predicate = () => File.Exists(components[1])
                                        });
                                break;
                                case "file-not-exists":
                                    instructions.Add(new ParsedInstruction
                                        {
                                            Predicate = () => !File.Exists(components[1])
                                        });
                                    break;
                                default:
                                    throw new ParserErrorException(
                                        "Unexpected if condition, expected 'host', 'host-not', 'host-in', 'host-not-in', " + 
                                        "'file-exists' or 'file-not-exists', " +
                                        "got '" + components[0] + "' instead", currentLine);
                            }
                            break;
                        }
                        case "endif":
                            instructions.Add(new ParsedInstruction
                            {
                                EndPredicate = true
                            });
                            break;
                        case "resolve":
                        case "generate":
                        case "sync":
                        case "resync":
                        case "build":
                        case "execute":
                        case "native-execute":
                        case "pack":
                        case "push":
                        case "repush":
                        case "nuget":
                            instructions.Add(new ParsedInstruction
                            {
                                Command = instruction[0].ToLower(),
                                Arguments = instruction.Length >= 2 ? instruction[1] : string.Empty
                            });
                            break;
                        case "echo":
                            instructions.Add(new ParsedInstruction
                            {
                                Echo = instruction.Length >= 2 ? instruction[1] : string.Empty
                            });
                            break;
                        case "set":
                        {
                            if (instruction.Length == 1)
                            {
                                throw new ParserErrorException(
                                    "Expected variable name, set line missing entirely",
                                    currentLine);
                            }

                            var components = instruction[1].Split(' ');
                            switch (components[0])
                            {
                                case "target-platforms":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Key = "target-platforms",
                                        Values = components.Skip(1).ToArray()
                                    });
                                    break;
                                case "build-target":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Key = "build-target",
                                        Values = components.Skip(1).ToArray()
                                    });
                                    break;
                                case "build-property":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Key = "build-property",
                                        Values = components.Skip(1).ToArray()
                                    });
                                    break;
                                case "build-process-arch":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Key = "build-process-arch",
                                        Values = components.Skip(1).ToArray()
                                    });
                                    break;
                                case "execute-configuration":
                                    instructions.Add(new ParsedInstruction
                                    {
                                        Key = "execute-configuration",
                                        Values = components.Skip(1).ToArray()
                                    });
                                    break;
                                default:
                                    throw new ParserErrorException(
                                        "Unexpected variable name, expected 'target-platforms', 'build-target', 'build-property', " +
                                        "'build-process-arch' or 'execute-configuration', got '" +
                                        components[0] + "' instead", currentLine);
                            }
                            break;
                        }
                        default:
                            throw new ParserErrorException(
                                "Unexpected command in script '" +
                                instruction[0].ToLower() + "'", currentLine);
                    }
                }
                catch (ParserErrorException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new ParserErrorException(ex, currentLine);
                }
            }

            return instructions;
        }

        public int Execute(object handle)
        {
            var instructions = (List<ParsedInstruction>) handle;

            var protobuild = Assembly.GetEntryAssembly().Location;
            var workingDirectory = _workingDirectoryProvider.GetPath();

            var targets = string.Empty;
            var buildTarget = string.Empty;
            var buildProcessArch = string.Empty;
            var buildProperties = new Dictionary<string, string>();
            string executeConfiguration = null;

            var predicates = new Stack<ParsedInstruction>();
            foreach (var inst in instructions)
            {
                if (inst.Predicate != null)
                {
                    predicates.Push(inst);
                }
                else if (inst.EndPredicate)
                {
                    predicates.Pop();
                }
                else if (predicates.All(x => x.Predicate()))
                {
                    if (inst.Command == "native-execute")
                    {
                        var components = inst.Arguments.Split(new[] {' '}, 2);

                        string path;
                        try
                        {
                            path = FindNativeProgram(components[0]);
                        }
                        catch (ApplicationException ex)
                        {
                            Console.Error.WriteLine(ex);
                            return 1;
                        }

                        var args = components.Length == 2 ? components[1] : string.Empty;

                        Console.WriteLine("+ native-execute " + path + " " + args);
                        var process =
                            Process.Start(new ProcessStartInfo(path, args)
                            {
                                WorkingDirectory = workingDirectory,
                                UseShellExecute = false
                            });
                        if (process == null)
                        {
                            Console.Error.WriteLine("ERROR: Process did not start when running " + path + " " +
                                args);
                            return 1;
                        }
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                        {
                            Console.Error.WriteLine(
                                "ERROR: Non-zero exit code " + process.ExitCode);
                            return process.ExitCode;
                        }
                    }
                    else if (inst.Command == "nuget")
                    {
                        // See if we have a copy of NuGet available for use.
                        var cachedNuget =
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "NuGet.exe");
                        if (!File.Exists(cachedNuget))
                        {
                            var client = new RetryableWebClient();
                            client.DownloadFile("https://dist.nuget.org/win-x86-commandline/latest/nuget.exe", cachedNuget);
                        }

                        string runtime = null;
                        var hostPlatform = _hostPlatformDetector.DetectPlatform();
                        if (hostPlatform != "Windows")
                        {
                            try
                            {
                                runtime = FindNativeProgram("mono");
                            }
                            catch (ApplicationException ex)
                            {
                                Console.Error.WriteLine(ex);
                                return 1;
                            }
                        }
                        
                        Process process;

                        if (hostPlatform != "Windows" && runtime != null)
                        {
                            Console.WriteLine("+ " + runtime + " \"" + cachedNuget + "\" " + inst.Arguments);
                            process =
                                Process.Start(new ProcessStartInfo(runtime, "\"" + cachedNuget + "\" " + inst.Arguments)
                                {
                                    WorkingDirectory = workingDirectory,
                                    UseShellExecute = false
                                });
                        }
                        else
                        {
                            Console.WriteLine("+ " + cachedNuget + " " + inst.Arguments);
                            process =
                                Process.Start(new ProcessStartInfo(cachedNuget, inst.Arguments)
                                {
                                    WorkingDirectory = workingDirectory,
                                    UseShellExecute = false
                                });
                        }

                        if (process == null)
                        {
                            Console.Error.WriteLine(
                                "ERROR: Process did not start when running NuGet with arguments " + inst.Arguments);
                            return 1;
                        }
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                        {
                            Console.Error.WriteLine(
                                "ERROR: Non-zero exit code " + process.ExitCode);
                            return process.ExitCode;
                        }
                    }
                    else if (inst.Command != null)
                    {
                        var args = string.Empty;
                        switch (inst.Command)
                        {
                            case "execute":
                                if (executeConfiguration != null)
                                {
                                    args = "--execute-configuration " + executeConfiguration + " --" + inst.Command +
                                           " " + inst.Arguments;
                                }
                                else
                                {
                                    args = "--" + inst.Command + " " + inst.Arguments;
                                }
                                break;
                            case "build":
                                args = "--" + inst.Command + " " + targets + " ";

                                if (buildTarget != string.Empty)
                                {
                                    args += "--build-target " + buildTarget + " ";
                                }

                                if (buildProcessArch != string.Empty)
                                {
                                    args += "--build-process-arch " + buildProcessArch + " ";
                                }

                                args = buildProperties.Aggregate(args,
                                    (current, prop) =>
                                        current + ("--build-property " + prop.Key + " " + prop.Value + " "));
                                args += " " + inst.Arguments;
                                break;
                            case "pack":
                            case "push":
                            case "repush":
                                args = "--" + inst.Command + " " + inst.Arguments;
                                break;
                            default:
                                args = "--" + inst.Command + " " + targets + " " + inst.Arguments;
                                break;
                        }

                        var runSets = new List<string>();
                        if (args.Contains("$TARGET_PLATFORM"))
                        {
                            var targetsSplit = targets.Split(',');
                            if (targetsSplit.Length < 1 || string.IsNullOrWhiteSpace(targetsSplit[0]))
                            {
                                runSets.Add(_hostPlatformDetector.DetectPlatform());
                            }
                            else
                            {
                                runSets.AddRange(targetsSplit);
                            }
                        }
                        else
                        {
                            runSets.Add(_hostPlatformDetector.DetectPlatform());
                        }

                        if (args.Contains("$GIT_BRANCH"))
                        {
                            Console.Error.WriteLine(
                                "ERROR: Support for $GIT_BRANCH has been dropped, because it almost never behaved as intended (due to no guarentees that the desired refs would exist in the repository being operated on).");
                            return 1;
                        }

                        if (args.Contains("$GIT_COMMIT"))
                        {
                            string commit;

                            try
                            {
                                Console.WriteLine("+ git rev-parse HEAD");
                                commit = GitUtils.RunGitAndCapture(workingDirectory, "rev-parse HEAD").Trim();
                            }
                            catch (InvalidOperationException)
                            {
                                commit = string.Empty;
                            }

                            args = args.Replace("$GIT_COMMIT", commit);
                        }

                        string runtime = null;
                        var hostPlatform = _hostPlatformDetector.DetectPlatform();
                        if (hostPlatform != "Windows")
                        {
                            try
                            {
                                runtime = FindNativeProgram("mono");
                            }
                            catch (ApplicationException ex)
                            {
                                Console.Error.WriteLine(ex);
                                return 1;
                            }
                        }

                        foreach (var run in runSets)
                        {
                            var runArgs = args
                                .Replace("$TARGET_PLATFORM", run);

                            Process process;

                            if (hostPlatform != "Windows" && runtime != null)
                            {
                                Console.WriteLine("+ " + runtime + " \"" + protobuild + "\" " + runArgs);
                                process =
                                    Process.Start(new ProcessStartInfo(runtime, "\"" + protobuild + "\" " + runArgs)
                                        {
                                            WorkingDirectory = workingDirectory,
                                            UseShellExecute = false
                                        });
                            }
                            else
                            {
                                Console.WriteLine("+ " + protobuild + " " + runArgs);
                                process =
                                    Process.Start(new ProcessStartInfo(protobuild, runArgs)
                                    {
                                        WorkingDirectory = workingDirectory,
                                        UseShellExecute = false
                                    });
                            }

                            if (process == null)
                            {
                                Console.Error.WriteLine(
                                    "ERROR: Process did not start when running Protobuild with arguments " + args);
                                return 1;
                            }
                            process.WaitForExit();
                            if (process.ExitCode != 0)
                            {
                                Console.Error.WriteLine(
                                    "ERROR: Non-zero exit code " + process.ExitCode);
                                return process.ExitCode;
                            }
                        }
                    }
                    else if (inst.Key != null)
                    {
                        Console.WriteLine("+ set " + inst.Key + " -> " + inst.Values.Aggregate((a, b) => a + ", " + b));
                        switch (inst.Key)
                        {
                            case "target-platforms":
                                targets = inst.Values.Aggregate((a, b) => a + "," + b);
                                break;
                            case "build-target":
                                buildTarget = inst.Values.First();
                                break;
                            case "build-process-arch":
                                buildProcessArch = inst.Values.First();
                                break;
                            case "build-property":
                                buildProperties.Add(inst.Values[0],
                                    inst.Values.Length >= 2 ? inst.Values[1] : string.Empty);
                                break;
                            case "execute-configuration":
                                executeConfiguration = inst.Values.First();
                                break;
                        }
                    }
                    else if (inst.Echo != null)
                    {
                        Console.WriteLine(inst.Echo);
                    }
                }
            }

            Console.WriteLine("Automated build script completed successfully.");
            return 0;
        }

        private string FindNativeProgram(string program)
        {
            var search = _hostPlatformDetector.DetectPlatform() == "Windows"
                ? new[] {@"C:\Windows\System32\where.exe"}
                : new[] {"/usr/bin/which", "/bin/which"};
            string searchFound = null;
            foreach (var s in search)
            {
                if (File.Exists(s))
                {
                    searchFound = s;
                    break;
                }
            }

            if (searchFound == null)
            {
                throw new ApplicationException("ERROR: Could not find which or where on your system.");
            }

            Console.WriteLine("+ native-execute " + searchFound + " " + program);
            var searchProcess =
                Process.Start(new ProcessStartInfo(searchFound, program)
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    });
            if (searchProcess == null)
            {
                throw new ApplicationException("ERROR: Process did not start when searching for " + program);
            }
            var path = searchProcess.StandardOutput.ReadToEnd().Split('\r', '\n').First();

            if (string.IsNullOrWhiteSpace(path) && _hostPlatformDetector.DetectPlatform() == "MacOS")
            {
                if (File.Exists("/usr/local/bin/" + program))
                {
                    // After upgrading to OSX El Capitan, the /usr/local/bin folder is no longer in
                    // the system PATH.  If we can't find Mono with the which tool, manually set the
                    // path here in an attempt to find it.
                    path = "/usr/local/bin/" + program;
                }
            }

            if (!File.Exists(path))
            {
                throw new ApplicationException("ERROR: Located file '" + path + "' for " + program +
                    " does not actually exist.");
            }

            return path;
        }

        private class ParsedInstruction
        {
            public string Arguments;

            public string Command;

            public bool EndPredicate;

            public string Key;
            public Func<bool> Predicate;

            public string[] Values;
            public string Echo;
        }

        private class ParserErrorException : Exception
        {
            public ParserErrorException(Exception innerException, int currentLine)
                : base(
                    innerException.Message.TrimEnd('.') + " on line " + currentLine + " of script." +
                    Environment.NewLine + innerException.StackTrace, innerException)
            {
            }

            public ParserErrorException(string message, int currentLine)
                : base(message.TrimEnd('.') + " on line " + currentLine + " of script.")
            {
            }
        }
    }
}