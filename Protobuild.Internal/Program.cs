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

    class MainClass
    {
        public static void Main(string[] args)
        {
            // Ensure we always use the invariant culture in Protobuild.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var commandMappings = new Dictionary<string, ICommand>
            {
                { "sync", new SyncCommand() },
                { "resync", new ResyncCommand() },
                { "generate", new GenerateCommand() },
                { "clean", new CleanCommand() },
                { "extract-xslt", new ExtractXSLTCommand() },
                { "enable", new EnableServiceCommand() },
                { "disable", new DisableServiceCommand() },
                { "spec", new ServiceSpecificationCommand() },
                { "add", new AddPackageCommand() },
                { "pack", new PackPackageCommand() },
                { "format", new FormatPackageCommand() },
                { "push", new PushPackageCommand() },
                { "start", new StartCommand() },
            };

            var execution = new Execution();
            execution.CommandToExecute = new DefaultCommand();

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
                Environment.Exit(0);
            };
            options["help"] = helpAction;
            options["?"] = helpAction;

            try
            {
                options.Parse(args);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                PrintHelp(commandMappings);
                Environment.Exit(1);
            }

            try
            {
                var exitCode = execution.CommandToExecute.Execute(execution);
                Environment.Exit(exitCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Environment.Exit(1);
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
                    }

                    if (count >= 74)
                    {
                        lines.Add(lineBuffer);
                        lineBuffer = string.Empty;
                        count = 0;
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
