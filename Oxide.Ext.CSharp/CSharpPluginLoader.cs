using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class CSharpPluginLoader : PluginLoader
    {
        public static string[] DefaultReferences = { "mscorlib", "System", "System.Core", "System.Data", "Oxide.Core", "Oxide.Ext.CSharp" };
        public static HashSet<string> PluginReferences = new HashSet<string>(DefaultReferences);
        public static CSharpPluginLoader Instance;
        private static Dictionary<string, CompilablePlugin> plugins = new Dictionary<string, CompilablePlugin>();
        private static CSharpExtension extension;

        public static CompilablePlugin GetCompilablePlugin(string directory, string name)
        {
            var class_name = Regex.Replace(Regex.Replace(name, @"(?:^|_)([a-z])", m => m.Groups[1].Value.ToUpper()), "_", "");
            CompilablePlugin plugin;
            if (!plugins.TryGetValue(class_name, out plugin))
            {
                plugin = new CompilablePlugin(extension, Instance, directory, name);
                plugins[class_name] = plugin;
            }
            return plugin;
        }
        
        private List<CompilablePlugin> compilationQueue = new List<CompilablePlugin>();
        private PluginCompiler compiler;

        public CSharpPluginLoader(CSharpExtension extension)
        {
            Instance = this;
            CSharpPluginLoader.extension = extension;
            PluginCompiler.CheckCompilerBinary();
            compiler = new PluginCompiler();
        }

        public void OnModLoaded()
        {
            // Include references to all loaded game extensions and any assemblies they reference
            foreach (var extension in Interface.Oxide.GetAllExtensions())
            {
                if (!extension.IsGameExtension) continue;
                var assembly = extension.GetType().Assembly;
                PluginReferences.Add(assembly.GetName().Name);
                foreach (var reference in assembly.GetReferencedAssemblies())
                    PluginReferences.Add(reference.Name);
            }
        }

        public override IEnumerable<string> ScanDirectory(string directory)
        {
            if (PluginCompiler.BinaryPath == null) yield break;
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
            if (LoadingPlugins.Contains(name))
            {
                Interface.Oxide.LogDebug("Plugin is already being loaded: {0}", name);
                return null;
            }

            // Let the Oxide core know that this plugin will be loading asynchronously
            LoadingPlugins.Add(name);

            var compilable_plugin = GetCompilablePlugin(directory, name);
            compilable_plugin.Compile(compiled =>
            {
                // Load the plugin assembly if it was successfully compiled
                if (compiled)
                    compilable_plugin.LoadPlugin(plugin =>
                    {
                        LoadingPlugins.Remove(name);
                        if (plugin != null) LoadedPlugins[compilable_plugin.Name] = plugin;
                    });
                else
                {
                    LoadingPlugins.Remove(name);
                    compilable_plugin.IsReloading = false;
                }
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
            var compilable_plugin = GetCompilablePlugin(directory, name);
            if (compilable_plugin.IsReloading)
            {
                Interface.Oxide.LogDebug("Reload requested for plugin which is already reloading: {0}", name);
                return;
            }
            compilable_plugin.IsReloading = true;
            compilable_plugin.Compile(compiled =>
            {
                if (!compiled)
                {
                    Interface.Oxide.LogError("Plugin failed to compile: {0} (leaving previous version loaded)", name);
                    compilable_plugin.IsReloading = false;
                    return;
                }
                Interface.Oxide.UnloadPlugin(name);
                // Delay load by a frame so that all plugins in a batch can be unloaded before the assembly is loaded
                Interface.Oxide.NextTick(() =>
                {
                    if (!Interface.Oxide.LoadPlugin(name)) compilable_plugin.IsReloading = false;
                });
            });
        }

        /// <summary>
        /// Called when the plugin manager is unloading a plugin that was loaded by this plugin loader
        /// </summary>
        /// <param name="plugin_base"></param>
        public override void Unloading(Plugin plugin_base)
        {
            var plugin = plugin_base as CSharpPlugin;
            LoadedPlugins.Remove(plugin.Name);
            // Unload plugins which require this plugin first
            foreach (var compilable_plugin in plugins.Values)
            {
                if (compilable_plugin.Requires.Contains(plugin.Name))
                    Interface.Oxide.UnloadPlugin(compilable_plugin.Name);
            }
        }

        /// <summary>
        /// Called when a CompilablePlugin wants to be compiled
        /// </summary>
        /// <param name="plugin"></param>
        public void CompilationRequested(CompilablePlugin plugin)
        {
            if (compilationQueue.Count < 1)
            {
                Interface.Oxide.NextTick(() =>
                {
                    CompileAssembly(compilationQueue.ToArray());
                    compilationQueue.Clear();
                });
            }
            plugin.IsCompilationNeeded = true;
            compilationQueue.Add(plugin);
            // Enqueue compilation of any plugins which depend on this plugin
            foreach (var compilable_plugin in plugins.Values.Where(pl => pl.Requires.Contains(plugin.Name)))
            {
                if (compilationQueue.Contains(compilable_plugin)) continue;
                compilable_plugin.CompiledAssembly = null;
                Reload(Interface.Oxide.PluginDirectory, compilable_plugin.Name);
            }
        }

        private void CompileAssembly(CompilablePlugin[] plugs)
        {
            foreach (var pl in plugs) pl.OnCompilationStarted(compiler);
            var plugins = new List<CompilablePlugin>(plugs);
            compiler.Compile(plugins, (assembly_name, raw_assembly, duration) =>
            {
                if (plugins.Count > 1 && raw_assembly == null)
                {
                    var plugin_names = plugins.Select(pl => pl.Name);
                    var standalone_plugins = plugins.Where(pl => !pl.Requires.Any(r => plugin_names.Contains(r))).ToArray();
                    foreach (var plugin in standalone_plugins) plugins.Remove(plugin);
                    foreach (var plugin in plugins)
                    {
                        plugin.OnCompilationFailed();
                        PluginErrors[plugin.Name] = "Batch containing dependencies failed to compile";
                    }
                    if (standalone_plugins.Length < 1)
                    {
                        Interface.Oxide.LogError($"A batch of {plugins.Count} plugins failed to compile");
                        return;
                    }
                    Interface.Oxide.LogError($"A batch of {plugins.Count} plugins failed to compile, attempting to compile separately");
                    foreach (var plugin in standalone_plugins) CompileAssembly(new[] { plugin });
                    return;
                }
                if (raw_assembly == null)
                {
                    var plugin = plugins[0];
                    plugin.OnCompilationFailed();
                    PluginErrors[plugin.Name] = "Failed to compile: " + plugin.CompilerErrors;
                    Interface.Oxide.LogError("{0} plugin failed to compile!", plugin.ScriptName);
                    Interface.Oxide.LogError(plugin.CompilerErrors);
                    RemoteLogger.Warning($"{plugin.ScriptName} plugin failed to compile!\n{plugin.CompilerErrors}");
                }
                else
                {
                    var compiled_plugins = plugins.Where(pl => pl.CompilerErrors == null).ToList();
                    var compiled_names = compiled_plugins.Select(pl => pl.Name).ToArray();
                    CompiledAssembly compiled_assembly = null;
                    if (compiled_plugins.Count > 0)
                    {
                        var verb = compiled_plugins.Count > 1 ? "were" : "was";
                        Interface.Oxide.LogInfo($"{compiled_names.ToSentence()} {verb} compiled successfully in {Math.Round(duration * 1000f)}ms");
                        compiled_assembly = new CompiledAssembly(assembly_name, compiled_plugins.ToArray(), raw_assembly);
                    }
                    foreach (var plugin in plugins)
                    {
                        if (plugin.CompilerErrors == null)
                        {
                            plugin.OnCompilationSucceeded(compiled_assembly);
                        }
                        else
                        {
                            plugin.OnCompilationFailed();
                            PluginErrors[plugin.Name] = "Failed to compile: " + plugin.CompilerErrors;
                            Interface.Oxide.LogError($"Error while compiling {plugin.CompilerErrors}");
                        }
                    }
                }
            });
        }

        public void OnShutdown()
        {
            compiler.OnShutdown();
        }
    }
}
