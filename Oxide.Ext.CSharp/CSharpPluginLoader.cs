using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class CSharpPluginLoader : PluginLoader
    {
        public static List<string> ProjectReferences;

        private CSharpExtension extension;
        private Dictionary<string, CompilablePlugin> plugins = new Dictionary<string, CompilablePlugin>();

        public CSharpPluginLoader(CSharpExtension extension)
        {
            this.extension = extension;

            // Plugins inherit all references from Oxide.Ext.CSharp
            ProjectReferences = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(r => r.Name).ToList();
            ProjectReferences.Add("Oxide.Ext.CSharp");

            // Check if compatible compiler is installed
            PluginCompiler.BinaryPath = Environment.GetEnvironmentVariable("systemroot") + "\\Microsoft.NET\\Framework64\\v3.5\\csc.exe";
            if (!File.Exists(PluginCompiler.BinaryPath))
            {
                Interface.GetMod().LogInfo("Error: Cannot compile C# plugins. Unable to find csc.exe! Have you installed .Net v3.5 x64?");
                PluginCompiler.BinaryPath = null;
                return;
            }

            // Delete any previously compiled temporary plugin files
            foreach (var path in Directory.GetFiles(Interface.GetMod().TempDirectory, "*.dll"))
                File.Delete(path);
        }
        
        public override IEnumerable<string> ScanDirectory(string directory)
        {
            //yield return "CSharpCore";
            foreach (string file in Directory.GetFiles(directory, "*.cs"))
                yield return Path.GetFileNameWithoutExtension(file);
        }
        
        /// <summary>
        /// Attempt to asynchronously compile and load plugin
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Plugin Load(string directory, string name)
        {
            //if (name == "CSharpCore") return new CSharpCore();

            if (LoadingPlugins.Contains(name))
            {
                Interface.GetMod().LogInfo("Plugin is already being loaded: {0}", name);
                return null;
            }

            // Let the Oxide core know that this plugin will be loading asynchronously
            LoadingPlugins.Add(name);

            var plugin = GetCompilablePlugin(extension, directory, name);
            plugin.Compile(compiled =>
            {
                // Load the plugin assembly if it was successfully compiled
                if (compiled)
                    plugin.LoadAssembly(loaded => LoadingPlugins.Remove(name));
                else
                    LoadingPlugins.Remove(name);
            });

            return null;
        }

        /// <summary>
        /// Attempt to asynchronously compile plugin and only reload if successful
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        public override void Reload(string directory, string name)
        {
            // Attempt to compile the plugin before unloading the old version
            GetCompilablePlugin(extension, directory, name).Compile(compiled =>
            {
                if (!compiled)
                {
                    Interface.GetMod().LogInfo("Plugin failed to compile: {0} (leaving previous version loaded)", name);
                    return;
                }
                Interface.GetMod().UnloadPlugin(name);
                Interface.GetMod().LoadPlugin(name);
            });
        }

        private CompilablePlugin GetCompilablePlugin(CSharpExtension extension, string directory, string name)
        {
            CompilablePlugin plugin;
            if (!plugins.TryGetValue(name, out plugin))
            {
                plugin = new CompilablePlugin(extension, directory, name);
                plugins[name] = plugin;
            }
            return plugin;
        }
    }
}
