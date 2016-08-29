using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Protobuild
{
    internal class FeatureManager : IFeatureManager
    {
        private readonly IModuleExecution _moduleExecution;

        private bool _parentPropagatesFullFeatureSet = false;

        private List<Feature> _featuresEnabledFromCommandLine = null;

        private List<Feature> _featuresEnabledFromModules = null;

        public FeatureManager(IModuleExecution moduleExecution)
        {
            _moduleExecution = moduleExecution;
        }

        public void LoadFeaturesFromCommandLine(string commandArguments)
        {
            if (commandArguments == null)
            {
                _parentPropagatesFullFeatureSet = false;
                _featuresEnabledFromCommandLine = null;
            }
            else if (commandArguments == "full")
            {
                _parentPropagatesFullFeatureSet = true;
                _featuresEnabledFromCommandLine = null;
            }
            else if (string.IsNullOrWhiteSpace(commandArguments))
            {
                _parentPropagatesFullFeatureSet = false;
                _featuresEnabledFromCommandLine = new List<Feature>();
            }
            else
            {
                _parentPropagatesFullFeatureSet = false;

                var commandArgumentsArr = commandArguments.Split(',');
                var commandArgumentsList = new List<Feature>();
                for (var i = 0; i < commandArgumentsArr.Length; i++)
                {
                    try
                    {
                        commandArgumentsList.Add((Feature) Enum.Parse(typeof(Feature), commandArgumentsArr[i]));
                    }
                    catch
                    {
                        Console.WriteLine("WARNING: Unknown feature specified on command line; ignoring: " +
                                          commandArgumentsArr[i]);
                    }
                }
                _featuresEnabledFromCommandLine = commandArgumentsList;
            }
        }

        public void LoadFeaturesForCurrentDirectory()
        {
            var modulePath = Path.Combine("Build", "Module.xml");
            LoadFeaturesFromSpecificModule(modulePath);
        }

        public void LoadFeaturesFromSpecificModule(ModuleInfo module)
        {
            if (module.FeatureSet == null)
            {
                _featuresEnabledFromModules = null;
            }
            else
            {
                _featuresEnabledFromModules = module.FeatureSet.ToList();
            }
        }

        public void LoadFeaturesFromSpecificModule(string path)
        {
            if (File.Exists(path))
            {
                LoadFeaturesFromSpecificModule(ModuleInfo.Load(path));
            }
        }

        public Feature[] GetAllEnabledFeatures()
        {
            if ((_featuresEnabledFromCommandLine == null && _featuresEnabledFromModules == null) || _parentPropagatesFullFeatureSet)
            {
                var features = new List<Feature>();

                foreach (var name in Enum.GetNames(typeof(Feature)))
                {
                    var field = typeof(Feature).GetField(name);
                    var internalAttribute = field.GetCustomAttributes(typeof(FeatureInternalAttribute), false).FirstOrDefault() as FeatureInternalAttribute;
                    if (internalAttribute == null)
                    {
                        features.Add((Feature)field.GetValue(null));
                    }
                }

                return features.ToArray();
            }

            var commandLineFeatures = (_featuresEnabledFromCommandLine ?? new List<Feature>()).ToArray();
            var moduleFeatures = (_featuresEnabledFromModules ?? new List<Feature>()).ToArray();

            return commandLineFeatures.Concat(moduleFeatures).Distinct().ToArray();
        }

        public string GetFeatureArgumentToPassToSubmodule(ModuleInfo module, ModuleInfo submodule)
        {
            if (!IsFeatureEnabledInSubmodule(module, submodule, Feature.PropagateFeatures))
            {
                // This submodule doesn't support feature propagation, so we pass no arguments.
                return string.Empty;
            }

            if ((_featuresEnabledFromCommandLine == null && _featuresEnabledFromModules == null) || _parentPropagatesFullFeatureSet)
            {
                return "--features full ";
            }

            var features = GetAllEnabledFeatures();
            if (features.Length == 0)
            {
                return "--features \"\" ";
            }

            return "--features \"" + features.Select(x => x.ToString()).Aggregate((a, b) => a + "," + b) + "\" ";
        }

        public bool IsFeatureEnabled(Feature feature)
        {
            // If the module has no restrictions, then the feature is enabled.
            if (_featuresEnabledFromModules == null)
            {
                return true;
            }

            // If we were invoked by the parent module and it specified the
            // full feature set that we support, then all features are supported.
            if (_parentPropagatesFullFeatureSet)
            {
                return true;
            }

            // If a feature is enabled from the command-line, then it's always 
            // enabled.  Parent modules will propagate their feature sets to
            // submodules to ensure that features they depend on get activated
            // for submodules as well.
            if (_featuresEnabledFromCommandLine != null)
            {
                if (_featuresEnabledFromCommandLine.Contains(feature))
                {
                    return true;
                }
            }

            // If a feature is enabled by the currently loaded module, then it
            // is enabled.
            if (_featuresEnabledFromModules.Contains(feature))
            {
                return true;
            }

            return false;
        }

        public bool IsFeatureEnabledInSubmodule(ModuleInfo module, ModuleInfo submodule, Feature feature)
        {
            if (submodule.CachedInternalFeatures == null)
            {
                var result = _moduleExecution.RunProtobuild(submodule, "-query-features", true);
                var exitCode = result.Item1;
                var stdout = result.Item2 + result.Item3;

                if (exitCode != 0 || stdout.Contains("Protobuild.exe [options]") || stdout.Contains("Unknown argument"))
                {
                    submodule.CachedInternalFeatures = new Feature[0];
                }

                submodule.CachedInternalFeatures = ParseFeaturesFromStdout(stdout);

                if (submodule.CachedInternalFeatures.Contains(Feature.PropagateFeatures))
                {
                    // If we have a limited feature set, we need to query again with
                    // only our features propagated.
                    result = _moduleExecution.RunProtobuild(submodule, GetFeatureArgumentToPassToSubmodule(module, submodule) + "-query-features", true);
                    exitCode = result.Item1;
                    stdout = result.Item2 + result.Item3;

                    if (!(exitCode != 0 || stdout.Contains("Protobuild.exe [options]") || stdout.Contains("Unknown argument")))
                    {
                        submodule.CachedInternalFeatures = ParseFeaturesFromStdout(stdout);
                    }
                }
            }

            return submodule.CachedInternalFeatures.Contains(feature);
        }

        private Feature[] ParseFeaturesFromStdout(string stdout)
        {
            var entries = stdout.Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                
            if (entries.Count == 0)
            {
                return new Feature[0];
            }
            else
            {
                return entries.Select(LookupFeatureByID).Where(x => x != null).Select(x => x.Value).ToArray();
            }
        }

        public string[] GetEnabledInternalFeatureIDs()
        {
            var internalFeatureIDs = new List<string>();

            foreach (var name in Enum.GetNames(typeof(Feature)))
            {
                var field = typeof(Feature).GetField(name);
                var internalAttribute = field.GetCustomAttributes(typeof(FeatureInternalAttribute), false).FirstOrDefault() as FeatureInternalAttribute;
                if (internalAttribute != null)
                {
                    var dependsOn = field.GetCustomAttributes(typeof(FeatureDependsOnAttribute), false).FirstOrDefault() as FeatureDependsOnAttribute;
                    if (dependsOn != null)
                    {
                        var enabled = GetAllEnabledFeatures();
                        var allowFeature = true;
                        foreach (var depend in dependsOn.DependsOn)
                        {
                            if (!enabled.Contains(depend))
                            {
                                allowFeature = false;
                                break;
                            }
                        }

                        if (!allowFeature)
                        {
                            continue;
                        }
                    }

                    internalFeatureIDs.Add(internalAttribute.InternalId);
                }
            }

            return internalFeatureIDs.ToArray();
        }

        private Feature? LookupFeatureByID(string str)
        {
            foreach (var name in Enum.GetNames(typeof(Feature)))
            {
                var field = typeof(Feature).GetField(name);
                var internalAttribute = field.GetCustomAttributes(typeof(FeatureInternalAttribute), false).FirstOrDefault() as FeatureInternalAttribute;
                if (internalAttribute != null)
                {
                    if (internalAttribute.InternalId == str)
                    {
                        return (Feature)field.GetValue(null);
                    }
                }
            }

            Console.Error.WriteLine("WARNING: Unable to find feature based on ID '" + str + "'");
            return null;
        }

        private Feature[] GetAllNonInternalFeatures()
        {
            var features = new List<Feature>();

            foreach (var name in Enum.GetNames(typeof(Feature)))
            {
                var field = typeof(Feature).GetField(name);
                var internalAttribute = field.GetCustomAttributes(typeof(FeatureInternalAttribute), false).FirstOrDefault() as FeatureInternalAttribute;
                if (internalAttribute == null)
                {
                    features.Add((Feature)field.GetValue(null));
                }
            }

            return features.ToArray();
        }

        public void ValidateEnabledFeatures()
        {
            var features = GetAllEnabledFeatures().OrderByDescending(x => (int)x).ToArray();
            var nonInternalFeatures = GetAllNonInternalFeatures().OrderByDescending(x => (int)x).ToList();
            var missingFeatures = new List<Feature>();

            foreach (var enabledFeature in features)
            {
                var idx = nonInternalFeatures.IndexOf(enabledFeature);
                if (idx == -1)
                {
                    continue;
                }

                for (var i = idx; i < nonInternalFeatures.Count; i++)
                {
                    if (!features.Contains(nonInternalFeatures[i]))
                    {
                        if (!missingFeatures.Contains(nonInternalFeatures[i]))
                        {
                            missingFeatures.Add(nonInternalFeatures[i]);
                        }
                    }
                }
            }

            if (missingFeatures.Count > 0)
            {
                var featureList = features.Length == 0 ? string.Empty : features.Select(x => x.ToString()).Aggregate((a, b) => a + "," + b);
                var missingFeatureList = missingFeatures.Count == 0 ? string.Empty : missingFeatures.Select(x => x.ToString()).Aggregate((a, b) => a + "," + b);

                Console.Error.WriteLine(
                    "WARNING: The active feature set is missing previous features!  " +
                    "You have the following features enabled: '" + 
                    featureList + 
                    "', but the following features should also be enabled for stability reasons: '" + 
                    missingFeatureList + "'");
            }
        }
    }
}

