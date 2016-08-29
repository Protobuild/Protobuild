using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Protobuild
{
    using System.Collections.Generic;
    using System.IO.Compression;

    /// <summary>
    /// The main entry point for Protobuild.  This class resides in a library, not an executable, so
    /// another assembly such as Protobuild or Protobuild.Debug needs to invoke the main method on this
    /// class.
    /// </summary>
    public static class MainClass
    {
        /// <summary>
        /// The entry point for Protobuild.
        /// </summary>
        /// <param name="args">The arguments passed in on the command line.</param>
        public static void Main(string[] args)
        {
            // Ensure we always use the invariant culture in Protobuild.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            // Set our SSL trust policy.  Because Mono doesn't ship with root certificates
            // on most Linux distributions, we have to be a little more insecure here than
            // I'd like.  For protobuild.org we always verify that the root of the certificate
            // chain matches what we expect (so people can't forge a certificate from a
            // *different CA*), but for other domains we just have to implicitly trust them
            // on Linux since we have no root store.
            if (Path.DirectorySeparatorChar == '/' && !Directory.Exists("/Library"))
            {
                ServicePointManager.ServerCertificateValidationCallback = SSLValidationForLinux;
            }

            var kernel = new LightweightKernel();
            kernel.BindCore();
            kernel.BindBuildResources();
            kernel.BindGeneration();
            kernel.BindJSIL();
            kernel.BindTargets();
            kernel.BindFileFilter();
            kernel.BindPackages();
            kernel.BindAutomatedBuild();

            var featureManager = kernel.Get<IFeatureManager>();
            featureManager.LoadFeaturesForCurrentDirectory();

            var commandMappings = new Dictionary<string, ICommand>
            {
                { "sync", kernel.Get<SyncCommand>() },
                { "resync", kernel.Get<ResyncCommand>() },
                { "generate", kernel.Get<GenerateCommand>() },
                { "build", kernel.Get<BuildCommand>() },
                { "build-target", kernel.Get<BuildTargetCommand>() },
                { "build-property", kernel.Get<BuildPropertyCommand>() },
                { "build-process-arch", kernel.Get<BuildProcessArchCommand>() },
                { "clean", kernel.Get<CleanCommand>() },
                { "automated-build", kernel.Get<AutomatedBuildCommand>() },
                { "extract-xslt", kernel.Get<ExtractXSLTCommand>() },
                { "enable", kernel.Get<EnableServiceCommand>() },
                { "disable", kernel.Get<DisableServiceCommand>() },
                { "debug-service-resolution", kernel.Get<DebugServiceResolutionCommand>() },
                { "simulate-host-platform", kernel.Get<SimulateHostPlatformCommand>() },
                { "spec", kernel.Get<ServiceSpecificationCommand>() },
                { "query-features", kernel.Get<QueryFeaturesCommand>() },
                { "features", kernel.Get<FeaturesCommand>() },
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
                { "safe-resolve", kernel.Get<SafeResolveCommand>() },
                { "parallel", kernel.Get<ParallelCommand>() },
                { "no-parallel", kernel.Get<NoParallelCommand>() },
                { "redirect", kernel.Get<RedirectPackageCommand>() },
                { "swap-to-source", kernel.Get<SwapToSourceCommand>() },
                { "swap-to-binary", kernel.Get<SwapToBinaryCommand>() },
                { "start", kernel.Get<StartCommand>() },
                { "no-generate", kernel.Get<NoGenerateCommand>() },
                { "no-host-generate", kernel.Get<NoHostGenerateCommand>() },
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

                Action<string[]> handle = x =>
                {
                    if (value.IsRecognised())
                    {
                        value.Encounter(execution, x); 
                    }
                    else if (value.IsIgnored())
                    {
                    }
                    else
                    {
                        throw new InvalidOperationException("Unknown argument '" + key + "'");
                    }
                };

                if (value.GetArgCount() == 0)
                {
                    options[key] = handle;
                }
                else
                {
                    options[key + "@" + value.GetArgCount()] = handle;
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

            featureManager.ValidateEnabledFeatures();

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

        private static bool SSLValidationForLinux(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            // If the Mono root CA store is initialized properly, then use that.
            if (sslpolicyerrors == SslPolicyErrors.None)
            {
                // Good certificate.
                return true;
            }

            // I'm not sure of the stability of the certificate thumbprint for protobuild.org
            // itself, so instead just verify that it is Let's Encrypt that's providing the
            // certificate.
            if (sender is HttpWebRequest &&
                (sender as HttpWebRequest).Host == "protobuild.org")
            {
                if (chain.ChainElements.Count > 2 &&
                    chain.ChainElements[1].Certificate.Thumbprint == "3EAE91937EC85D74483FF4B77B07B43E2AF36BF4")
                {
                    // This is the Let's Encrypt certificate authority.  We can implicitly trust this without warning.
                    return true;
                }
                else
                {
                    // The thumbprint differs!  Show a danger message to the user, but continue anyway
                    // because if the thumbprint does legitimately change, we have no way of backporting
                    // a new certificate thumbprint without issuing a new version of Protobuild.
                    var givenThumbprint = chain.ChainElements.Count >= 2 ?
                        chain.ChainElements[1].Certificate.Thumbprint :
                        "<no thumbprint available>";
                    Console.Error.WriteLine(
                        "DANGER: The thumbprint of the issuer's SSL certificate for protobuild.org \"" +
                        givenThumbprint + "\" does not match the expected thumbprint value \"" +
                        chain.ChainElements[1].Certificate.Thumbprint +
                        "\".  It's possible that Let's Encrypt " +
                        "changed their certificate thumbprint, or someone is performing a MITM " +
                        "attack on your connection.  Unfortunately Mono does not ship out-of-the-box " +
                        "with appropriate root CA certificates on Linux, so we have no method of verifying " +
                        "that the proposed thumbprint is correct.  You should verify that the given " +
                        "thumbprint is correct either through your web browser (by visiting protobuild.org " +
                        "and checking the certificate chain), or by performing the same operation on " +
                        "Mac OS or Windows.  If the operation succeeds, or the thumbprint matches, please " +
                        "file an issue at https://github.com/hach-que/Protobuild/issues/new so we can " +
                        "update the embedded thumbprint.  We will now continue the operation regardless " +
                        "as we can't update the thumbprint in previous versions if it has changed.");
                    return true;
                }
            }

            Console.WriteLine(
                "WARNING: Implicitly trusting SSL certificate " + certificate.GetCertHashString() + " " +
                "for " + certificate.Subject + " issued by " + certificate.Issuer + " on Linux, due " +
                "to inconsistent root CA store policies of Mono.");
            return true;
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
                if (kv.Value.IsInternal() || !kv.Value.IsRecognised() || kv.Value.IsIgnored())
                {
                    continue;
                }

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
