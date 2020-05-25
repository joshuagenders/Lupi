using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Yakka
{
    public class Plugin
    {
        private readonly string _assemblyPath;
        private Assembly _assembly {get;set;}
        public Plugin(string path)
        {
            _assemblyPath = path;
            LoadAssembly();
        }

        private void LoadAssembly()
        {
            if (_assembly == null)
            {
                PluginLoadContext loadContext = new PluginLoadContext(_assemblyPath);
                _assembly = loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(_assemblyPath)));
            }
        }

        public IRunner GetRunner()
        {
            return null;
        }
    }
}
