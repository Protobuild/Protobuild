//-----------------------------------------------------------------------
// <copyright file="ModuleInfo.cs" company="Protobuild Project">
// The MIT License (MIT)
// 
// Copyright (c) Various Authors
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
//     The above copyright notice and this permission notice shall be included in
//     all copies or substantial portions of the Software.
// 
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//     IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//     FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//     AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//     LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//     OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//     THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Protobuild
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Serialization;

    /// <summary>
    /// Represents a Protobuild module.
    /// </summary>
    [Serializable]
    public class ModuleInfo
    {
        /// <summary>
        /// The root path of this module.
        /// </summary>
        [NonSerialized]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute(
            "Microsoft.StyleCop.CSharp.Maintainability", 
            "SA1401:FieldsMustBePrivate", 
            Justification = "This must be a field to allow usage of the NonSerialized attribute.")]
        public string Path;

        /// <summary>
        /// Initializes a new instance of the <see cref="Protobuild.ModuleInfo"/> class.
        /// </summary>
        public ModuleInfo()
        {
            this.ModuleAssemblies = new string[0];
            this.DefaultAction = "resync";
            this.GenerateNuGetRepositories = true;
        }

        /// <summary>
        /// Gets or sets the name of the module.
        /// </summary>
        /// <value>The module name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the default action to be taken when Protobuild runs in the module.
        /// </summary>
        /// <value>The default action that Protobuild will take.</value>
        public string DefaultAction { get; set; }

        /// <summary>
        /// Gets or sets a comma seperated list of default platforms when the host platform is Windows.
        /// </summary>
        /// <value>The comma seperated list of default platforms when the host platform is Windows.</value>
        public string DefaultWindowsPlatforms { get; set; }

        /// <summary>
        /// Gets or sets a comma seperated list of default platforms when the host platform is Mac OS.
        /// </summary>
        /// <value>The comma seperated list of default platforms when the host platform is Mac OS.</value>
        public string DefaultMacOSPlatforms { get; set; }

        /// <summary>
        /// Gets or sets a comma seperated list of default platforms when the host platform is Linux.
        /// </summary>
        /// <value>The comma seperated list of default platforms when the host platform is Linux.</value>
        public string DefaultLinuxPlatforms { get; set; } 

        /// <summary>
        /// Gets or sets a value indicating whether the NuGet repositories.config file is generated.
        /// </summary>
        /// <value><c>true</c> if the NuGet repositories.config file is generated; otherwise, <c>false</c>.</value>
        public bool GenerateNuGetRepositories { get; set; }

        /// <summary>
        /// Gets or sets a comma seperated list of supported platforms in this module.
        /// </summary>
        /// <value>The comma seperated list of supported platforms.</value>
        public string SupportedPlatforms { get; set; } 

        /// <summary>
        /// Gets or sets a value indicating whether synchronisation is completely disabled in this module.
        /// </summary>
        /// <value><c>true</c> if synchronisation is disabled and will be skipped; otherwise, <c>false</c>.</value>
        public bool? DisableSynchronisation { get; set; }

        /// <summary>
        /// Gets or sets the list of .NET assemblies to load when running Protobuild in this module.
        /// </summary>
        /// <remarks>
        /// This list of assemblies is used to load additional templates in the GUI-based module manager.
        /// </remarks>
        /// <value>The list of assemblies to load.</value>
        public string[] ModuleAssemblies { get; set; }

        /// <summary>
        /// Gets or sets the name of the default startup project.
        /// </summary>
        /// <value>The name of the default startup project.</value>
        public string DefaultStartupProject { get; set; } 

        /// <summary>
        /// Loads the Protobuild module from the Module.xml file.
        /// </summary>
        /// <param name="xmlFile">The path to a Module.xml file.</param>
        /// <returns>The loaded Protobuild module.</returns>
        public static ModuleInfo Load(string xmlFile)
        {
            var serializer = new XmlSerializer(typeof(ModuleInfo));
            var reader = new StreamReader(xmlFile);
            var module = (ModuleInfo)serializer.Deserialize(reader);
            module.Path = new FileInfo(xmlFile).Directory.Parent.FullName;
            reader.Close();
            return module;
        }

        /// <summary>
        /// Gets the list of available project templates for usage in the GUI-based module manager.
        /// </summary>
        /// <returns>The available project templates.</returns>
        public BaseTemplate[] GetTemplates()
        {
            return (from assembly in this.ModuleAssemblies
                    let loaded = Assembly.LoadFile(assembly)
                    from type in loaded.GetTypes()
                    where !type.IsAbstract
                    where type.GetConstructor(Type.EmptyTypes) != null
                    select Activator.CreateInstance(type) as BaseTemplate).ToArray();
        }

        /// <summary>
        /// Loads all of the project definitions present in the current module.
        /// </summary>
        /// <returns>The loaded project definitions.</returns>
        public DefinitionInfo[] GetDefinitions()
        {
            var result = new List<DefinitionInfo>();
            var path = System.IO.Path.Combine(this.Path, "Build", "Projects");
            foreach (var file in new DirectoryInfo(path).GetFiles("*.definition"))
            {
                result.Add(DefinitionInfo.Load(file.FullName));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Loads all of the project definitions present in the current module and all submodules.
        /// </summary>
        /// <returns>The loaded project definitions.</returns>
        /// <param name="relative">The current directory being scanned.</param>
        public IEnumerable<DefinitionInfo> GetDefinitionsRecursively(string relative = "")
        {
            foreach (var definition in this.GetDefinitions())
            {
                definition.Path = (relative + '\\' + definition.Path).Trim('\\');
                definition.ModulePath = this.Path;
                yield return definition;
            }

            foreach (var submodule in this.GetSubmodules())
            {
                foreach (var definition in submodule.GetDefinitionsRecursively((relative + '\\' + submodule.Name).Trim('\\')))
                {
                    yield return definition;
                }
            }
        }

        /// <summary>
        /// Loads all of the submodules present in this module.
        /// </summary>
        /// <returns>The loaded submodules.</returns>
        public ModuleInfo[] GetSubmodules()
        {
            var modules = new List<ModuleInfo>();
            foreach (var directory in new DirectoryInfo(this.Path).GetDirectories())
            {
                var build = directory.GetDirectories().FirstOrDefault(x => x.Name == "Build");
                if (build == null)
                {
                    continue;
                }

                var module = build.GetFiles().FirstOrDefault(x => x.Name == "Module.xml");
                if (module == null)
                {
                    continue;
                }

                modules.Add(ModuleInfo.Load(module.FullName));
            }

            return modules.ToArray();
        }

        /// <summary>
        /// Saves the current module to a Module.xml file.
        /// </summary>
        /// <param name="xmlFile">The path to a Module.xml file.</param>
        public void Save(string xmlFile)
        {
            var serializer = new XmlSerializer(typeof(ModuleInfo));
            var writer = new StreamWriter(xmlFile);
            serializer.Serialize(writer, this);
            writer.Close();
        }

        /// <summary>
        /// Runs the instance of Protobuild.exe present in the module.
        /// </summary>
        /// <param name="args">The arguments to pass to Protobuild.</param>
        public void RunProtobuild(string args)
        {
            var protobuildPath = System.IO.Path.Combine(this.Path, "Protobuild.exe");
            if (File.Exists(protobuildPath))
            {
                var pi = new ProcessStartInfo
                {
                    FileName = protobuildPath,
                    Arguments = args,
                    WorkingDirectory = this.Path,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var p = new Process { StartInfo = pi };
                p.OutputDataReceived += (sender, eventArgs) =>
                {
                    if (!string.IsNullOrEmpty(eventArgs.Data))
                    {
                        Console.WriteLine(eventArgs.Data);
                    }
                };
                p.ErrorDataReceived += (sender, eventArgs) =>
                {
                    if (!string.IsNullOrEmpty(eventArgs.Data))
                    {
                        Console.Error.WriteLine(eventArgs.Data);
                    }
                };
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
            }
        }

        /// <summary>
        /// Normalizes the platform string from user input, automatically correcting case
        /// and validating against a list of supported platforms.
        /// </summary>
        /// <returns>The platform string.</returns>
        /// <param name="platform">The normalized platform string.</param>
        public string NormalizePlatform(string platform)
        {
            var supportedPlatforms = "Android,iOS,Linux,MacOS,Ouya,PCL,PSMobile,Windows,Windows8,WindowsGL,WindowsPhone,WindowsPhone81";
            var defaultPlatforms = true;

            if (!string.IsNullOrEmpty(this.SupportedPlatforms))
            {
                supportedPlatforms = this.SupportedPlatforms;
                defaultPlatforms = false;
            }

            var supportedPlatformsArray = supportedPlatforms.Split(new[] { ',' })
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();

            // Search the array to find a platform that matches case insensitively
            // to the specified platform.  If we are using the default list, then we allow
            // other platforms to be specified (in case the developer has modified the XSLT to
            // support others but is not using <SupportedPlatforms>).  If the developer has
            // explicitly set the supported platforms, then we return null if the user passes
            // an unknown platform (the caller is expected to exit at this point).
            foreach (var supportedPlatform in supportedPlatformsArray)
            {
                if (string.Compare(supportedPlatform, platform, true) == 0)
                {
                    return supportedPlatform;
                }
            }

            if (defaultPlatforms)
            {
                return platform;
            }
            else
            {
                return null;
            }
        }
    }
}
