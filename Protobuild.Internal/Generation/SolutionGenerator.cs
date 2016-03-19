using System;
using System.Collections.Generic;
using Protobuild.Services;
using System.Xml;
using System.IO;
using System.Linq;

namespace Protobuild
{
    internal class SolutionGenerator : ISolutionGenerator
    {
        private readonly IResourceProvider m_ResourceProvider;

        private readonly ISolutionInputGenerator m_SolutionInputGenerator;

        private readonly INuGetRepositoriesConfigGenerator m_NuGetRepositoriesConfigGenerator;

        public SolutionGenerator(
            IResourceProvider resourceProvider,
            ISolutionInputGenerator solutionInputGenerator,
            INuGetRepositoriesConfigGenerator nuGetRepositoriesConfigGenerator)
        {
            this.m_ResourceProvider = resourceProvider;
            this.m_SolutionInputGenerator = solutionInputGenerator;
            this.m_NuGetRepositoriesConfigGenerator = nuGetRepositoriesConfigGenerator;
        }

        public void Generate(
            ModuleInfo moduleInfo,
            List<XmlDocument> definitions,
            string platformName,
            string solutionPath,
            List<Service> services,
            IEnumerable<string> repositoryPaths)
        {
            var generateSolutionTransform = this.m_ResourceProvider.LoadXSLT(ResourceType.GenerateSolution, Language.CSharp, platformName);
            var selectSolutionTransform = this.m_ResourceProvider.LoadXSLT(ResourceType.SelectSolution, Language.CSharp, platformName);

            var input = this.m_SolutionInputGenerator.GenerateForSelectSolution(definitions, platformName, services);
            using (var memory = new MemoryStream())
            {
                selectSolutionTransform.Transform(input, null, memory);

                memory.Seek(0, SeekOrigin.Begin);

                var document = new XmlDocument();
                document.Load(memory);

                var defaultProject = (XmlElement)null;
                var existingGuids = new List<string>();
                var existingFolders = new List<string>();
                foreach (var element in document.DocumentElement.SelectNodes("/Projects/Project").OfType<XmlElement>().ToList())
                {
                    var f = element.SelectNodes("ProjectGuids/Platform[@Name='" + platformName + "']").OfType<XmlElement>().FirstOrDefault();

                    if (f != null)
                    {
                        if (existingGuids.Contains(f.InnerText.Trim()))
                        {
                            element.ParentNode.RemoveChild(element);
                            continue;
                        }
                        else
                        {
                            existingGuids.Add(f.InnerText.Trim());
                        }
                    }

                    var s = element.SelectNodes("Guid").OfType<XmlElement>().FirstOrDefault();

                    if (s != null)
                    {
                        if (existingGuids.Contains(s.InnerText.Trim()))
                        {
                            element.ParentNode.RemoveChild(element);
                            continue;
                        }
                        else
                        {
                            existingGuids.Add(s.InnerText.Trim());
                        }
                    }

                    var d = element.SelectNodes("Folder").OfType<XmlElement>().FirstOrDefault();

                    if (d != null)
                    {
                        if (!existingFolders.Contains(s.InnerText.Trim()))
                        {
                            existingFolders.Add(s.InnerText.Trim());
                        }
                    }

                    var n = element.SelectNodes("RawName").OfType<XmlElement>().FirstOrDefault();

                    if (n != null)
                    {
                        if (n.InnerText.Trim() == moduleInfo.DefaultStartupProject)
                        {
                            defaultProject = element;
                        }
                    }
                }

                if (defaultProject != null)
                {
                    // Move the default project to the first element of it's parent.  The first project
                    // in the solution is the default startup project.
                    var parent = defaultProject.ParentNode;
                    parent.RemoveChild(defaultProject);
                    parent.InsertBefore(defaultProject, parent.FirstChild);
                }

                //foreach (var folder in existingFolders)
                //{
                //    //var FolderProject = new 
                //    document.CreateElement()
                //    document.SelectSingleNode("/Projects")
                //}

                var documentInput = this.m_SolutionInputGenerator.GenerateForGenerateSolution(
                    platformName,
                    document.DocumentElement.SelectNodes("/Projects/Project").OfType<XmlElement>());

                using (var writer = new StreamWriter(solutionPath))
                {
                    generateSolutionTransform.Transform(documentInput, null, writer);
                }
            }

            if (repositoryPaths != null && repositoryPaths.Any())
            {
                this.m_NuGetRepositoriesConfigGenerator.Generate(solutionPath, repositoryPaths);
            }
        }
    }
}

