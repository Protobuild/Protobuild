using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Protobuild
{
    public class LightweightKernel
    {
        private Dictionary<Type, Type> m_Bindings = new Dictionary<Type, Type>();

        private Dictionary<string, Type> m_NamedBindings = new Dictionary<string, Type>();

        private Dictionary<Type, bool> m_KeepInstance = new Dictionary<Type, bool>();

        private Dictionary<Type, object> m_Instances = new Dictionary<Type, object>();

        public void Bind<TInterface, TImplementation>() where TImplementation : TInterface
        {
            this.m_Bindings.Add(typeof(TInterface), typeof(TImplementation));
            this.m_KeepInstance.Add(typeof(TInterface), false);
        }

        public void BindAndKeepInstance<TInterface, TImplementation>() where TImplementation : TInterface
        {
            this.m_Bindings.Add(typeof(TInterface), typeof(TImplementation));
            this.m_KeepInstance.Add(typeof(TInterface), true);
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

            if (seen.Contains(original))
            {
                throw new InvalidOperationException(
                    "Attempting to resolve " + 
                    original.FullName + 
                    ", but it has already been seen during resolution.");
            }

            seen.Add(original);

            Type actual;

            if (this.m_Bindings.ContainsKey(original))
            {
                if (this.m_KeepInstance[original])
                {
                    if (this.m_Instances.ContainsKey(original))
                    {
                        return this.m_Instances[original];
                    }
                }

                actual = this.m_Bindings[original];
            }
            else
            {
                actual = original;
            }

            if (actual.IsInterface)
            {
                throw new InvalidOperationException("Resolved type " + actual.FullName + " was an interface; make sure there is a binding present!");
            }

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

            return instance;
        }
    }
}

