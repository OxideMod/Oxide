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
        private List<CompilablePlugin> compilationQueue = new List<CompilablePlugin>();

        public CSharpPluginLoader(CSharpExtension extension)
        {
            this.extension = extension;

            // Plugins inherit all references from Oxide.Ext.CSharp
            ProjectReferences = new HashSet<string>(Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(r => r.Name));
            ProjectReferences.Add("System.Data");
            
            // Check if compatible compiler is installed
            PluginCompiler.BinaryPath = Interface.Oxide.RootDirectory + @"\CSharpCompiler.exe";
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
                Interface.Oxide.LogWarning("Plugin is already being loaded: {0}", name);
                return null;
            }

            // Let the Oxide core know that this plugin will be loading asynchronously
            LoadingPlugins.Add(name);

            var compilable_plugin = GetCompilablePlugin(extension, directory, name);
            compilable_plugin.Compile(compiled =>
            {
                // Load the plugin assembly if it was successfully compiled
                if (compiled)
                    compilable_plugin.LoadPlugin(plugin =>
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
                    Interface.Oxide.LogError("Plugin failed to compile: {0} (leaving previous version loaded)", name);
                    return;
                }
                Interface.Oxide.UnloadPlugin(name);
                Interface.Oxide.LoadPlugin(name);
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

        /// <summary>
        /// Called when a CompilablePlugin wants to be compiled
        /// </summary>
        /// <param name="plugin"></param>
        public void CompilationRequested(CompilablePlugin plugin)
        {
            compilationQueue.Add(plugin);
            if (compilationQueue.Count > 1) return;
            Interface.Oxide.NextTick(() =>
            {
                CompileAssembly(compilationQueue.ToArray());
                compilationQueue.Clear();
            });
        }
        
        private void CompileAssembly(CompilablePlugin[] plugins)
        {
            var compiler = new PluginCompiler(plugins);
            foreach (var pl in plugins) pl.OnCompilationStarted(compiler);
            compiler.Compile(raw_assembly =>
            {
                var plugin_names = string.Join(", ", compiler.Plugins.Select(pl => pl.Name).ToArray());
                if (compiler.Plugins.Count > 1 && raw_assembly == null)
                {
                    Interface.Oxide.LogError($"A batch of {compiler.Plugins.Count} plugins failed to compile, attempting to compile separately");
                    foreach (var plugin in compiler.Plugins) CompileAssembly(new[] { plugin });
                    return;
                }
                if (raw_assembly == null)
                {
                    foreach (var plugin in compiler.Plugins) plugin.OnCompilationFailed();
                }
                else
                {
                    Interface.Oxide.LogInfo("{0} {1} compiled successfully in {2}ms", plugin_names, compiler.Plugins.Count > 1 ? "were" : "was", Math.Round(compiler.Duration * 1000f));
                    var compiled_assembly = new CompiledAssembly(compiler.Plugins.ToArray(), raw_assembly);
                    foreach (var plugin in compiler.Plugins) plugin.OnCompilationSucceeded(compiled_assembly);
                }
            });
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
