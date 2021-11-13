using Autofac;
using Autofac.Extensions.DependencyInjection;
using Lupi.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Lupi.Core
{
    public class AssemblyPlugin : IPlugin
    {
        private readonly Config _config;
        private readonly CancellationToken _ct;
        private readonly Assembly _assembly;
        private readonly IServiceProvider _ioc;

        private readonly MethodInfo _testMethod;
        private readonly MethodInfo _setupMethod;
        private readonly MethodInfo _teardownMethod;

        private object _testClassSingleton;
        private Func<Task<object>> _testFunc { get; set; }

        
        public AssemblyPlugin(Config config, CancellationToken ct = default)
        {
            _config = config;
            _ct = ct;

            PluginLoadContext loadContext = new (_config.Test.AssemblyPath);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(_config.Test.AssemblyPath));

            _assembly = loadContext.LoadFromAssemblyName(assemblyName);
            _testMethod = GetMethod(_config.Test.TestClass, _config.Test.TestMethod);
            _setupMethod = GetMethod(_config.Test.SetupClass, _config.Test.SetupMethod);
            _teardownMethod = GetMethod(_config.Test.TeardownClass, _config.Test.TeardownMethod);
            _ioc = GetServiceProvider().GetAwaiter().GetResult();
            if (_config.Test.SingleTestClassInstance)
            {
                _testClassSingleton = GetInstance(_config.Test.TestClass);
            }
            _testFunc = GetMethodRunner(_testMethod, () => GetTestClassInstance());
        }

        private object GetTestClassInstance()
        {
            if (_testMethod.IsStatic)
            {
                return null;
            }
            return _config.Test.SingleTestClassInstance
                ? _testClassSingleton
                : GetInstanceFromType(_testMethod.DeclaringType);
        }

        private Func<Task<object>> GetMethodRunner(MethodInfo method, Func<object> classInstanceProvider)
        {
            if (method == null)
            {
                throw new ArgumentException("Test method not found");
            }

            if (!IsAsyncMethod(method))
            {
                return new Func<Task<object>>(() => Task.FromResult(method.Invoke(classInstanceProvider(), GetParameters(method))));
            }
            
            if (_testMethod.ReturnType.GetGenericArguments().Any())
            {
                return new Func<Task<object>>(async () =>
                {
                    var task = (Task)method.Invoke(
                        classInstanceProvider(), 
                        GetParameters(method));
                    await task;
                    if (task.IsFaulted)
                    {
                        return task.Exception;
                    }
                    return task.GetType().GetProperty("Result").GetValue(task);
                });
            }
            else
            {
                return new Func<Task<object>>(async () =>
                {
                    var task = (Task)method.Invoke(
                        classInstanceProvider(), 
                        GetParameters(method));
                    await task;
                    if (task.IsFaulted)
                    {
                        return task.Exception;
                    }
                    return null;
                });
            }
        }

        private static bool IsAsyncMethod(MethodInfo method) =>
            (AsyncStateMachineAttribute)method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;

        private async Task<IServiceProvider> GetServiceProvider()
        {  
            var startup = _assembly
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.ReturnType == typeof(IServiceProvider) 
                    || m.ReturnType.GetGenericArguments().FirstOrDefault() == typeof(IServiceProvider))
                .FirstOrDefault();

            if (startup == null)
            {
                var builder = new ContainerBuilder();
                builder.RegisterAssemblyTypes(_assembly)
                       .AsImplementedInterfaces();
                var serviceCollection = new ServiceCollection();
                builder.Populate(serviceCollection);
                return serviceCollection.BuildServiceProvider();
            }
            else
            {
                var instance = startup.IsStatic ? null : GetInstanceFromType(startup.DeclaringType);
                return (IServiceProvider)await GetMethodRunner(startup, () => instance)();
            }
        }

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
            if (type.IsInterface && _ioc != null)
            {
                return _ioc.GetService(type);
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
                var m = c.GetMethods()
                    .Where(m => m.Name.Equals(method))
                    .OrderBy(m => m.GetParameters().Count())
                    .FirstOrDefault();

                if (m == null)
                {
                    throw new ArgumentException($"Method not found. {classFullName}.{method}");
                }
                return m;
            }
            else if (!string.IsNullOrWhiteSpace(classFullName))
            {
                throw new ArgumentException($"Class not found. {classFullName}");
            }
            return null;
        }

        public async Task<object> ExecuteSetupMethod()
        {
            if (_setupMethod != null)
            {
                var instance = _setupMethod.IsStatic ? null : GetInstanceFromType(_setupMethod.DeclaringType);
                return await GetMethodRunner(_setupMethod, () => instance)();
            }
            return null;
        } 
            
        public async Task<object> ExecuteTestMethod() =>
            await _testFunc.Invoke();

        public async Task<object> ExecuteTeardownMethod() 
        {
            if (_teardownMethod != null)
            {
                var instance = _teardownMethod.IsStatic ? null : GetInstanceFromType(_teardownMethod.DeclaringType);
                return await GetMethodRunner(_teardownMethod, () => instance)();
            }
            return null;
        }
    }

    public interface IPlugin
    {
        Task<object> ExecuteSetupMethod();
        Task<object> ExecuteTestMethod();
        Task<object> ExecuteTeardownMethod();
    }
}
