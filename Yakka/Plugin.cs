using Autofac;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Yakka
{
    public class Plugin
    {
        private readonly Config _config;
        private Assembly _assembly { get; set; }
        public Plugin(Config config)
        {
            _config = config;
            LoadAssembly();
        }

        private void LoadAssembly()
        {
            if (_assembly == null)
            {
                PluginLoadContext loadContext = new PluginLoadContext(_config.Test.AssemblyPath);
                var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(_config.Test.AssemblyPath));
                _assembly = loadContext.LoadFromAssemblyName(assemblyName);
            }
        }

        private ContainerBuilder GetIocBuilder()
        {
            var builder = new ContainerBuilder();
            builder.RegisterAssemblyTypes(_assembly)
                   .AsImplementedInterfaces();
            return builder;
        }

        //todo -> also pass to startup class if matching siignature / configured
        public IContainer GetIocContainer() => 
            GetIocBuilder().Build();

        public object GetInstance(string className)
        {
            var c = GetClass(className);
            if (c == null)
            {
                return null;
            }
            return GetIocContainer().Resolve(c);
        }

        public Type GetClass(string className) => 
            _assembly.GetTypes().Where(t => t.FullName.Equals(className)).FirstOrDefault();

        public MethodInfo GetMethod(string classFullName, string method)
        {
            var c = GetClass(classFullName);
            if (c != null)
            {
                var m = c.GetMethods().Where(m => m.Name.Equals(method));
                return m.FirstOrDefault();
            }
            return null;
        }
    }
}
