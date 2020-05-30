using Autofac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Yakka
{
    public class Plugin : IPlugin
    {
        private readonly Config _config;
        private readonly CancellationToken _ct;
        private readonly Assembly _assembly;
        private readonly IContainer _ioc;

        private readonly object _testClassSingleton;
        private readonly Func<Task<object>> _testFunc;

        private readonly MethodInfo _testMethod;
        
        public Plugin(Config config, CancellationToken ct = default)
        {
            _config = config;
            _ct = ct;

            PluginLoadContext loadContext = new PluginLoadContext(_config.Test.AssemblyPath);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(_config.Test.AssemblyPath));

            _assembly = loadContext.LoadFromAssemblyName(assemblyName);

            _testMethod = GetMethod(_config.Test.TestClass, _config.Test.TestMethod);
            
            
            //todo move to function - dont need readonly testfunc - generic func cache?
            if (_testMethod.IsStatic)
            {
                _testFunc = new Func<Task<object>>(() => Task.FromResult(_testMethod.Invoke(null, GetParameters(_testMethod))));
            }
            else
            {
                //todo - how to handle overloaded methods - prefer those with resolvable type inputs?
                if (_config.Test.SingleTestClassInstance)
                {
                    _testClassSingleton = GetInstance(_config.Test.TestClass);
                    _testFunc = new Func<Task<object>>(async () => await RunMethod(_testMethod, _testClassSingleton, GetParameters(_testMethod)));  
                }
                else
                {
                    _testFunc = new Func<Task<object>>(async () => await RunMethod(_testMethod, GetInstance(_config.Test.TestClass), GetParameters(_testMethod)));
                }
            }
            // end

            _ioc = GetIocContainer();
        }

        public async Task<object> RunMethod(MethodInfo method, object instance, object[] parameters)
        {
            if (IsAsyncMethod(_testMethod))
            {
                if (_testMethod.ReturnType.GetGenericArguments().Any())
                {
                    var task = (Task)method.Invoke(instance, parameters);
                    await task;
                    var resultProperty = typeof(Task<>)
                        .MakeGenericType(_testMethod.ReturnType.GetGenericArguments().First())
                        .GetProperty("Result");
                    object result = resultProperty.GetValue(task);
                    return result;
                }
                else
                {
                    var task = (Task)method.Invoke(instance, parameters);
                    await task;
                    return null;
                }
            }
            else
            {
                var result = method.Invoke(instance, parameters);
                return result;
            }
        }

        private static bool IsAsyncMethod(MethodInfo method) =>
            (AsyncStateMachineAttribute)method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;

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

        private object[] GetParameters(MethodInfo method)
        {
            var parms = method.GetParameters()
                .Select(p => p.ParameterType)
                .Select(GetInstanceFromType)
                .ToArray();
            return parms.Any()
                ? parms
                : null;
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

        public object ExecuteTestMethod() => _testFunc.Invoke().GetAwaiter().GetResult();
    }

    public interface IPlugin
    {
        object ExecuteTestMethod();
    }
}
