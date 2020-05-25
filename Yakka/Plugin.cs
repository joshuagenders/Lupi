using Autofac;
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

        public IContainer GetIocContainer() => 
            GetIocBuilder().Build();

        public MethodInfo GetMethod(string classFullName, string method)
        {
            var c = _assembly.GetTypes().Where(t => t.FullName.Equals(classFullName));
            if (c.Any())
            {
                var m = c.First().GetMethods().Where(m => m.Name.Equals(method));
                return m.FirstOrDefault();
            }
            return null;
        }
    }
}
