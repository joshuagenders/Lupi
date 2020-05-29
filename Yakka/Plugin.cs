using Autofac;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Yakka
{
    public class Plugin : IPlugin
    {
        private readonly Config _config;
        private readonly Assembly _assembly;
        private readonly object _classMethodSingleton;
        private readonly Action _setupAction; //todo
        private readonly Action _testAction;
        private readonly Action _teardownAction; //todo
        private readonly MethodInfo _testMethod;
        public Plugin(Config config)
        {
            _config = config;

            PluginLoadContext loadContext = new PluginLoadContext(_config.Test.AssemblyPath);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(_config.Test.AssemblyPath));

            _assembly = loadContext.LoadFromAssemblyName(assemblyName);

            _testMethod = GetMethod(_config.Test.TestClass, _config.Test.TestMethod);
            if (_testMethod.IsStatic)
            {
                _testAction = new Action(() => _testMethod.Invoke(null, null));
            }
            else
            {
                //todo method inputs -> timers, cancellation tokens, registered assembly interfaces
                if (_config.Test.SingleTestClassInstance)
                {
                    _classMethodSingleton = GetInstance(_config.Test.TestClass);
                    _testAction = new Action(() => _testMethod.Invoke(_classMethodSingleton, null));
                }
                else
                {
                    _testAction = new Action(() => _testMethod.Invoke(GetInstance(_config.Test.TestClass), null));
                }
            }
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
            return GetIocContainer().Resolve(c);
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

        public void ExecuteTestMethod() =>_testAction.Invoke();
    }

    public interface IPlugin
    {
        void ExecuteTestMethod();
    }
}
