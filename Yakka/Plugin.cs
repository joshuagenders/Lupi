using Autofac;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Yakka
{
    public class Plugin : IPlugin
    {
        private readonly Config _config;
        private readonly CancellationToken _ct;
        private readonly Assembly _assembly;
        private readonly object _classMethodSingleton;
        private readonly Action _setupAction; //todo
        private readonly Func<object> _testFunc;
        private readonly Action _teardownAction; //todo
        private readonly MethodInfo _testMethod;
        private readonly IContainer _ioc;

        public Plugin(Config config, CancellationToken ct = default)
        {
            _config = config;
            _ct = ct;

            PluginLoadContext loadContext = new PluginLoadContext(_config.Test.AssemblyPath);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(_config.Test.AssemblyPath));

            _assembly = loadContext.LoadFromAssemblyName(assemblyName);

            _testMethod = GetMethod(_config.Test.TestClass, _config.Test.TestMethod);
            if (_testMethod.IsStatic)
            {
                _testFunc = new Func<object>(() => _testMethod.Invoke(null, null));
            }
            else
            {
                //todo method inputs -> timers, cancellation tokens, registered assembly interfaces, default constructor or null
                //handle overloaded methods - prefer those with resolvable type inputs?
                if (_config.Test.SingleTestClassInstance)
                {
                    _classMethodSingleton = GetInstance(_config.Test.TestClass);
                    _testFunc = new Func<object>(() => _testMethod.Invoke(_classMethodSingleton, null));
                }
                else
                {
                    _testFunc = new Func<object>(() => _testMethod.Invoke(GetInstance(_config.Test.TestClass), null));
                }
            }

            _ioc = GetIocContainer();
        }

        private ContainerBuilder GetIocBuilder()
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(_assembly)
                   .AsImplementedInterfaces();
            return builder;
        }

        //todo -> also pass to startup class if matching signature / configured
        private IContainer GetIocContainer() =>
            GetIocBuilder().Build();

        private object GetInstance(string className)
        {
            var c = GetClass(className);
            if (c == null)
            {
                return null;
            }
            var instance = GetInstanceFromType(c);
            return instance;
        }

        private object GetInstanceFromType(Type type)
        {
            if (type.Equals(typeof(CancellationToken)))
            {
                return _ct;
            }
            if (type.IsInterface)
            {
                return _ioc.Resolve(type);
            }
            var defaultConstructor = type.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor == null)
            {
                var inputs = new List<object>();
                var constructor = type.GetConstructors().OrderBy(c => c.GetParameters().Count()).First();
                foreach (var input in constructor.GetParameters())
                {
                    inputs.Add(GetInstanceFromType(input.ParameterType));
                }
                return Activator.CreateInstance(type, inputs.ToArray());
            }
            else
            {
                return Activator.CreateInstance(type);
            }
        }

        private Type GetClass(string className) =>
            _assembly.GetTypes().Where(t => t.FullName.Equals(className)).FirstOrDefault();

        private MethodInfo GetMethod(string classFullName, string method)
        {
            var c = GetClass(classFullName);
            if (c != null)
            {
                var m = c.GetMethods().Where(m => m.Name.Equals(method));
                return m.FirstOrDefault();
            }
            return null;
        }

        public object ExecuteTestMethod() => _testFunc.Invoke();
    }

    public interface IPlugin
    {
        object ExecuteTestMethod();
    }
}
