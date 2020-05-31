using Autofac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lupi.Configuration;

namespace Lupi
{
    public class Plugin : IPlugin
    {
        private readonly Config _config;
        private readonly CancellationToken _ct;
        private readonly Assembly _assembly;
        private readonly IContainer _ioc;

        private readonly MethodInfo _testMethod;
        private readonly MethodInfo _setupMethod;
        private readonly MethodInfo _teardownMethod;

        private object _testClassSingleton;
        private Func<Task<object>> _testFunc { get; set; }

        
        public Plugin(Config config, CancellationToken ct = default)
        {
            _config = config;
            _ct = ct;

            PluginLoadContext loadContext = new PluginLoadContext(_config.Test.AssemblyPath);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(_config.Test.AssemblyPath));

            _assembly = loadContext.LoadFromAssemblyName(assemblyName);
            _testMethod = GetMethod(_config.Test.TestClass, _config.Test.TestMethod);
            _setupMethod = GetMethod(_config.Test.AssemblySetupClass, _config.Test.AssemblySetupMethod);
            _teardownMethod = GetMethod(_config.Test.AssemblyTeardownClass, _config.Test.AssemblyTeardownMethod);
            _ioc = GetIocContainer();
            SetupTestFunc();
        }

        private void SetupTestFunc()
        {
            //todo refactor this
            if (_testMethod.IsStatic)
            {
                _testFunc = new Func<Task<object>>(() =>
                   Task.FromResult(_testMethod.Invoke(null, GetParameters(_testMethod))));
            }
            if (IsAsyncMethod(_testMethod))
            {
                if (_config.Test.SingleTestClassInstance)
                {
                    _testClassSingleton = GetInstance(_config.Test.TestClass);
                    if (_testMethod.ReturnType.GetGenericArguments().Any())
                    {
                        _testFunc = new Func<Task<object>>(async () =>
                        {
                            var task = (Task)_testMethod.Invoke(
                                _testClassSingleton, 
                                GetParameters(_testMethod));
                            await task;
                            var resultProperty = typeof(Task<>)
                                .MakeGenericType(_testMethod.ReturnType.GetGenericArguments().First())
                                .GetProperty("Result");
                            object result = resultProperty.GetValue(task);
                            return result;
                        });
                    }
                    else
                    {
                        _testFunc = new Func<Task<object>>(async () =>
                        {
                            var task = (Task)_testMethod.Invoke(
                                _testClassSingleton, 
                                GetParameters(_testMethod));
                            await task;
                            return null;
                        });
                    }
                }
                else
                {
                    if (_testMethod.ReturnType.GetGenericArguments().Any())
                    {
                        _testFunc = new Func<Task<object>>(async () =>
                        {
                            var task = (Task)_testMethod.Invoke(
                                GetInstance(_config.Test.TestClass),
                                GetParameters(_testMethod));
                            await task;
                            var resultProperty = typeof(Task<>)
                                .MakeGenericType(_testMethod.ReturnType.GetGenericArguments().First())
                                .GetProperty("Result");
                            object result = resultProperty.GetValue(task);
                            return result;
                        });
                    }
                    else
                    {
                        _testFunc = new Func<Task<object>>(async () =>
                        {
                            var task = (Task)_testMethod.Invoke(
                                GetInstance(_config.Test.TestClass),
                                GetParameters(_testMethod));
                            await task;
                            return null;
                        });
                    }
                }
            }
            else
            {
                if (_config.Test.SingleTestClassInstance)
                {
                    _testClassSingleton = GetInstance(_config.Test.TestClass);
                    _testFunc = new Func<Task<object>>(() => 
                        Task.FromResult(_testMethod.Invoke(_testClassSingleton, GetParameters(_testMethod))));
                }
                else
                {
                    _testFunc = new Func<Task<object>>(() =>
                        Task.FromResult(_testMethod.Invoke(GetInstance(_config.Test.TestClass), GetParameters(_testMethod))));
                }
            }
        }

        public async Task<object> RunMethod(MethodInfo method, object[] parameters, object instance = null)
        {
            if (method.IsStatic)
            {
                return _testMethod.Invoke(null, GetParameters(_testMethod));
            }
            else if (instance == null)
            {
                instance = GetInstanceFromType(method.DeclaringType);
            }
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
            var startup = _assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => 
                    m.GetParameters().Any(p => p.ParameterType == typeof(ContainerBuilder))
                    && (m.ReturnType == typeof(ContainerBuilder) 
                        || m.ReturnType.GetGenericArguments().FirstOrDefault() == typeof(ContainerBuilder)))
                .FirstOrDefault();

            if (startup == null)
            {
                var builder = new ContainerBuilder();
                builder.RegisterAssemblyTypes(_assembly)
                       .AsImplementedInterfaces();
                return builder;
            }
            else
            {
                return (ContainerBuilder)RunMethod(startup, GetParameters(startup)).GetAwaiter().GetResult();
            }
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
            if (type.IsInterface || _ioc.IsRegistered(type))
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

        //todo - do we want to force sync?
        public object ExecuteSetupMethod() => 
            RunMethod(_setupMethod, GetParameters(_setupMethod)).GetAwaiter().GetResult();
        public object ExecuteTestMethod() => 
            _testFunc.Invoke().GetAwaiter().GetResult();
        public object ExecuteTeardownMethod() => 
            RunMethod(_teardownMethod, GetParameters(_teardownMethod)).GetAwaiter().GetResult();
    }

    public interface IPlugin
    {
        object ExecuteSetupMethod();
        object ExecuteTestMethod();
        object ExecuteTeardownMethod();
    }
}
