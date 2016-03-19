using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Protobuild
{
    internal class LightweightKernel
    {
        private Dictionary<Type, List<Type>> m_Bindings = new Dictionary<Type, List<Type>>();

        private Dictionary<string, Type> m_NamedBindings = new Dictionary<string, Type>();

        private Dictionary<Type, bool> m_KeepInstance = new Dictionary<Type, bool>();

        private Dictionary<Type, object> m_Instances = new Dictionary<Type, object>();

        /// <remarks>
        /// This is kind of ugly, but makes the code inside GenerationFunctions.cs much cleaner
        /// (since it doesn't have direct access to the LightweightKernelModule class).
        /// </remarks>
        public void BindAll()
        {
            this.BindCore();
            this.BindBuildResources();
            this.BindGeneration();
            this.BindJSIL();
            this.BindTargets();
            this.BindFileFilter();
            this.BindPackages();
        }

        public void Bind<TInterface, TImplementation>() where TImplementation : TInterface
        {
            if (!this.m_Bindings.ContainsKey(typeof (TInterface)))
            {
                this.m_Bindings.Add(typeof(TInterface), new List<Type>());
            }
            this.m_Bindings[typeof(TInterface)].Add(typeof(TImplementation));
            this.m_KeepInstance[typeof(TInterface)] = false;
        }

        public void BindAndKeepInstance<TInterface, TImplementation>() where TImplementation : TInterface
        {
            if (!this.m_Bindings.ContainsKey(typeof(TInterface)))
            {
                this.m_Bindings.Add(typeof(TInterface), new List<Type>());
            }
            this.m_Bindings[typeof(TInterface)].Add(typeof(TImplementation));
            this.m_KeepInstance[typeof(TInterface)] = true;
        }

        public T Get<T>()
        {
            return (T)this.Get(typeof(T), new List<Type>());
        }

        public T Get<T>(string name)
        {
            return (T)this.Get(this.m_NamedBindings[name], new List<Type>());
        }

        public object Get(Type t)
        {
            return this.Get(t, new List<Type>());
        }

        private object Get(Type original, List<Type> seen)
        {
            if (original == typeof(LightweightKernel))
            {
                return this;
            }

            var isArray = false;
            if (original.IsArray)
            {
                original = original.GetElementType();
                isArray = true;
            }

            if (seen.Contains(original))
            {
                throw new InvalidOperationException(
                    "Attempting to resolve " + 
                    original.FullName + 
                    ", but it has already been seen during resolution.");
            }

            seen.Add(original);

            Type[] actuals;

            if (this.m_Bindings.ContainsKey(original))
            {
                if (this.m_KeepInstance[original])
                {
                    if (this.m_Instances.ContainsKey(original))
                    {
                        return this.m_Instances[original];
                    }
                }

                actuals = this.m_Bindings[original].ToArray();
            }
            else
            {
                actuals = new [] { original };
            }

            if (actuals.All(x => x.IsInterface))
            {
                throw new InvalidOperationException("Resolved types " + actuals.Select(x => x.FullName).Aggregate((a, b) => a + ", " + b) + " were all interfaces; make sure there is a binding present!");
            }

            actuals = actuals.Where(x => !x.IsInterface).ToArray();

            var instances = new List<object>();
            foreach (var actual in actuals)
            {
                var constructor = actual.GetConstructors(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault();

                if (constructor == null)
                {
                    throw new InvalidOperationException("Type " + actual.FullName + " does not have a public constructor.");
                }

                var parameters = constructor.GetParameters();

                var resolved = new object[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;
                    resolved[i] = this.Get(parameterType, seen.ToList());
                }

                var instance = constructor.Invoke(resolved);

                if (this.m_Bindings.ContainsKey(original))
                {
                    if (this.m_KeepInstance[original])
                    {
                        if (!this.m_Instances.ContainsKey(original))
                        {
                            this.m_Instances[original] = instance;
                        }
                    }
                }

                instances.Add(instance);
            }

            if (isArray)
            {
                var arrayInstance = (Array)Activator.CreateInstance(original.MakeArrayType(), instances.Count);
                for (var i = 0; i < instances.Count; i++)
                {
                    arrayInstance.SetValue(instances[i], i);
                }
                return arrayInstance;
            }
            else
            {
                if (instances.Count > 1)
                {
                    throw new InvalidOperationException("More than one binding for " + original.FullName + " and non-array requested.");
                }

                return instances.First();
            }
        }
    }
}

