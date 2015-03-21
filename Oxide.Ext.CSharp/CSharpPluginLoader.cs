using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class CSharpPluginLoader : PluginLoader
    {
        public static HashSet<string> ProjectReferences;
        public List<CSharpPlugin> LoadedPlugins = new List<CSharpPlugin>();

        private CSharpExtension extension;
        private Dictionary<string, CompilablePlugin> plugins = new Dictionary<string, CompilablePlugin>();

        public CSharpPluginLoader(CSharpExtension extension)
        {
            this.extension = extension;

            // Plugins inherit all references from Oxide.Ext.CSharp
            ProjectReferences = new HashSet<string>(Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(r => r.Name));
            ProjectReferences.Add("System.Data");
            
            // Check if compatible compiler is installed
            PluginCompiler.BinaryPath = Interface.GetMod().RootDirectory + @"\CSharpCompiler.exe";
            if (!File.Exists(PluginCompiler.BinaryPath))
            {
                Interface.Oxide.LogError("Cannot compile C# plugins. Unable to find CSharpCompiler.exe!");
                PluginCompiler.BinaryPath = null;
                return;
            }
        }

        public void OnModLoaded()
        {
            // Include references to all loaded Oxide extensions and any assemblies they reference
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.FullName.StartsWith("Oxide.Ext")) continue;
                ProjectReferences.Add(assembly.GetName().Name);
                foreach (var reference in assembly.GetReferencedAssemblies())
                    ProjectReferences.Add(reference.Name);
            }
        }

        public override IEnumerable<string> ScanDirectory(string directory)
        {
            if (PluginCompiler.BinaryPath == null) yield break;
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

            var compilable_plugin = GetCompilablePlugin(extension, directory, name);
            compilable_plugin.Compile(compiled =>
            {
                // Load the plugin assembly if it was successfully compiled
                if (compiled)
                    compilable_plugin.LoadAssembly(plugin =>
                    {
                        LoadingPlugins.Remove(name);
                        if (plugin != null) LoadedPlugins.Add(plugin);
                    });
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

        /// <summary>
        /// Called when the plugin manager is unloading a plugin that was loaded by this plugin loader
        /// </summary>
        /// <param name="plugin"></param>
        public override void Unloading(Plugin plugin_base)
        {
            var plugin = plugin_base as CSharpPlugin;
            LoadedPlugins.Remove(plugin);
        }

        private CompilablePlugin GetCompilablePlugin(CSharpExtension extension, string directory, string name)
        {
            var class_name = Regex.Replace(Regex.Replace(name, @"(?:^|_)([a-z])", m => m.Groups[1].Value.ToUpper()), "_", "");
            CompilablePlugin plugin;
            if (!plugins.TryGetValue(class_name, out plugin))
            {
                plugin = new CompilablePlugin(extension, directory, name);
                plugins[class_name] = plugin;
            }
            return plugin;
        }
    }
}
