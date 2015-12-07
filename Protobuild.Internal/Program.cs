using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

namespace Protobuild
{
    using System.Collections.Generic;
    using System.IO.Compression;

    public static class MainClass
    {
        public static void Main(string[] args)
        {
            // Ensure we always use the invariant culture in Protobuild.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var kernel = new LightweightKernel();
            kernel.BindCore();
            kernel.BindBuildResources();
            kernel.BindGeneration();
            kernel.BindJSIL();
            kernel.BindTargets();
            kernel.BindFileFilter();
            kernel.BindPackages();
            kernel.BindAutomatedBuild();

            var commandMappings = new Dictionary<string, ICommand>
            {
                { "sync", kernel.Get<SyncCommand>() },
                { "resync", kernel.Get<ResyncCommand>() },
                { "generate", kernel.Get<GenerateCommand>() },
                { "build", kernel.Get<BuildCommand>() },
                { "build-target", kernel.Get<BuildTargetCommand>() },
                { "build-property", kernel.Get<BuildPropertyCommand>() },
                { "clean", kernel.Get<CleanCommand>() },
                { "automated-build", kernel.Get<AutomatedBuildCommand>() },
                { "extract-xslt", kernel.Get<ExtractXSLTCommand>() },
                { "enable", kernel.Get<EnableServiceCommand>() },
                { "disable", kernel.Get<DisableServiceCommand>() },
                { "debug-service-resolution", kernel.Get<DebugServiceResolutionCommand>() },
                { "simulate-host-platform", kernel.Get<SimulateHostPlatformCommand>() },
                { "spec", kernel.Get<ServiceSpecificationCommand>() },
                { "query-features", kernel.Get<QueryFeaturesCommand>() },
                { "add", kernel.Get<AddPackageCommand>() },
                { "list", kernel.Get<ListPackagesCommand>() },
                { "install", kernel.Get<InstallPackageCommand>() },
                { "upgrade", kernel.Get<UpgradePackageCommand>() },
                { "upgrade-all", kernel.Get<UpgradeAllPackagesCommand>() },
                { "pack", kernel.Get<PackPackageCommand>() },
                { "format", kernel.Get<FormatPackageCommand>() },
                { "push", kernel.Get<PushPackageCommand>() },
                { "ignore-on-existing", kernel.Get<IgnoreOnExistingPackageCommand>() },
                { "repush", kernel.Get<RepushPackageCommand>() },
                { "resolve", kernel.Get<ResolveCommand>() },
                { "no-resolve", kernel.Get<NoResolveCommand>() },
                { "redirect", kernel.Get<RedirectPackageCommand>() },
                { "swap-to-source", kernel.Get<SwapToSourceCommand>() },
                { "swap-to-binary", kernel.Get<SwapToBinaryCommand>() },
                { "start", kernel.Get<StartCommand>() },
                { "no-generate", kernel.Get<NoGenerateCommand>() },
                { "execute", kernel.Get<ExecuteCommand>() },
                { "execute-configuration", kernel.Get<ExecuteConfigurationCommand>() },
            };

            var execution = new Execution();
            execution.CommandToExecute = kernel.Get<DefaultCommand>();

            var options = new Options();
            foreach (var kv in commandMappings)
            {
                var key = kv.Key;
                var value = kv.Value;

                if (value.GetArgCount() == 0)
                {
                    options[key] = x => { value.Encounter(execution, x); };
                }
                else
                {
                    options[key + "@" + value.GetArgCount()] = x => { value.Encounter(execution, x); };
                }
            }

            Action<string[]> helpAction = x => 
            { 
                PrintHelp(commandMappings);
                ExecEnvironment.Exit(0);
            };
            options["help"] = helpAction;
            options["?"] = helpAction;

            if (ExecEnvironment.DoNotWrapExecutionInTry)
            {
                options.Parse(args);
            }
            else
            {
                try
                {
                    options.Parse(args);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex.Message);
                    PrintHelp(commandMappings);
                    ExecEnvironment.Exit(1);
                }
            }

            if (ExecEnvironment.DoNotWrapExecutionInTry)
            {
                var exitCode = execution.CommandToExecute.Execute(execution);
                ExecEnvironment.Exit(exitCode);
            }
            else
            {
                try
                {
                    var exitCode = execution.CommandToExecute.Execute(execution);
                    ExecEnvironment.Exit(exitCode);
                }
                catch (ExecEnvironment.SelfInvokeExitException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    ExecEnvironment.Exit(1);
                }
            }
        }

        private static void PrintHelp(Dictionary<string, ICommand> commandMappings)
        {
            Console.WriteLine("Protobuild.exe [options]");
            Console.WriteLine();
            Console.WriteLine("By default Protobuild resynchronises or generates projects for");
            Console.WriteLine("the current platform, depending on the module configuration.");
            Console.WriteLine();

            foreach (var kv in commandMappings)
            {
                var description = kv.Value.GetDescription();
                description = description.Replace("\n", " ");
                description = description.Replace("\r", "");

                var lines = new List<string>();
                var wordBuffer = string.Empty;
                var lineBuffer = string.Empty;
                var count = 0;
                var last = false;
                for (var i = 0; i < description.Length || wordBuffer.Length > 0; i++)
                {
                    if (i < description.Length)
                    {
                        if (description[i] == ' ')
                        {
                            if (wordBuffer.Length > 0)
                            {
                                lineBuffer += wordBuffer + " ";
                            }

                            wordBuffer = string.Empty;
                        }
                        else
                        {
                            wordBuffer += description[i];
                            count++;
                        }
                    }
                    else
                    {
                        lineBuffer += wordBuffer + " ";
                        count++;
                        last = true;
                    }

                    if (count >= 74)
                    {
                        lines.Add(lineBuffer);
                        lineBuffer = string.Empty;
                        count = 0;
                    }

                    if (last)
                    {
                        break;
                    }
                }

                if (count > 0)
                {
                    lines.Add(lineBuffer);
                    lineBuffer = string.Empty;
                }

                var argDesc = string.Empty;
                foreach (var arg in kv.Value.GetArgNames())
                {
                    if (arg.EndsWith("?"))
                    {
                        argDesc += " [" + arg.TrimEnd('?') + "]";
                    }
                    else
                    {
                        argDesc += " " + arg;
                    }
                }

                Console.WriteLine("  -" + kv.Key + argDesc);
                Console.WriteLine();

                foreach (var line in lines)
                {
                    Console.WriteLine("  " + line);
                }

                Console.WriteLine();
            }
        }
    }
}
