using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    public class CSharpPluginLoader : PluginLoader
    {
        public static string[] DefaultReferences = { "mscorlib", "Oxide.Core", "Oxide.CSharp", "System", "System.Core", "System.Data" };
        public static HashSet<string> PluginReferences = new HashSet<string>(DefaultReferences);
        public static CSharpPluginLoader Instance;
        private static Dictionary<string, CompilablePlugin> plugins = new Dictionary<string, CompilablePlugin>();
        private static CSharpExtension extension;

        public static CompilablePlugin GetCompilablePlugin(string directory, string name)
        {
            var className = Regex.Replace(name, "_", "");
            CompilablePlugin plugin;
            if (!plugins.TryGetValue(className, out plugin))
            {
                plugin = new CompilablePlugin(extension, Instance, directory, name);
                plugins[className] = plugin;
            }
            return plugin;
        }

        public override string FileExtension => ".cs";

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
                if (extension == null || !extension.IsCoreExtension && !extension.IsGameExtension) continue;

                var assembly = extension.GetType().Assembly;
                PluginReferences.Add(assembly.GetName().Name);
                foreach (var reference in assembly.GetReferencedAssemblies())
                    if (reference != null) PluginReferences.Add(reference.Name);
            }
        }

        public override IEnumerable<string> ScanDirectory(string directory)
        {
            if (PluginCompiler.BinaryPath == null) yield break;

            var enumerable = base.ScanDirectory(directory);
            foreach (var file in enumerable) yield return file;
        }

        /// <summary>
        /// Attempt to asynchronously compile and load plugin
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public override Plugin Load(string directory, string name)
        {
            var compilablePlugin = GetCompilablePlugin(directory, name);
            if (compilablePlugin.IsLoading)
            {
                Interface.Oxide.LogDebug($"Load requested for plugin which is already loading: {compilablePlugin.Name}");
                return null;
            }

            // Attempt to compile the plugin before unloading the old version
            Load(compilablePlugin);

            return null;
        }

        /// <summary>
        /// Attempt to asynchronously compile plugin and only reload if successful
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        public override void Reload(string directory, string name)
        {
            if (Regex.Match(directory, @"\\include\b", RegexOptions.IgnoreCase).Success)
            {
                name = $"Oxide.{name}";
                foreach (var plugin in plugins.Values)
                {
                    if (!plugin.References.Contains(name)) continue;
                    Interface.Oxide.LogInfo($"Reloading {plugin.Name} because it references updated include file: {name}");
                    plugin.LastModifiedAt = DateTime.Now;
                    Load(plugin);
                }
                return;
            }

            var compilablePlugin = GetCompilablePlugin(directory, name);
            if (compilablePlugin.IsLoading)
            {
                Interface.Oxide.LogDebug($"Reload requested for plugin which is already loading: {compilablePlugin.Name}");
                return;
            }

            // Attempt to compile the plugin before unloading the old version
            Load(compilablePlugin);
        }

        /// <summary>
        /// Called when the plugin manager is unloading a plugin that was loaded by this plugin loader
        /// </summary>
        /// <param name="pluginBase"></param>
        public override void Unloading(Plugin pluginBase)
        {
            var plugin = pluginBase as CSharpPlugin;
            if (plugin == null) return;

            LoadedPlugins.Remove(plugin.Name);

            // Unload plugins which require this plugin first
            foreach (var compilablePlugin in plugins.Values)
                if (compilablePlugin.Requires.Contains(plugin.Name)) Interface.Oxide.UnloadPlugin(compilablePlugin.Name);
        }

        public void Load(CompilablePlugin plugin)
        {
            plugin.Compile(compiled =>
            {
                if (!compiled)
                {
                    PluginLoadingCompleted(plugin);
                    return;
                }

                var loadedLoadingRequirements = plugin.Requires.Where(r => LoadedPlugins.ContainsKey(r) && LoadingPlugins.Contains(r));
                foreach (var loadedPlugin in loadedLoadingRequirements) Interface.Oxide.UnloadPlugin(loadedPlugin);

                var missingRequirements = plugin.Requires.Where(r => !LoadedPlugins.ContainsKey(r));
                if (missingRequirements.Any())
                {
                    var loadingRequirements = plugin.Requires.Where(r => LoadingPlugins.Contains(r));
                    if (loadingRequirements.Any())
                    {
                        Interface.Oxide.LogDebug($"{plugin.Name} plugin is waiting for requirements to be loaded: {loadingRequirements.ToSentence()}");
                    }
                    else
                    {
                        Interface.Oxide.LogError($"{plugin.Name} plugin requires missing dependencies: {missingRequirements.ToSentence()}");
                        PluginErrors[plugin.Name] = $"Missing dependencies: {missingRequirements.ToSentence()}";
                        PluginLoadingCompleted(plugin);
                    }
                }
                else
                {
                    Interface.Oxide.UnloadPlugin(plugin.Name);
                    plugin.LoadPlugin(pl =>
                    {
                        if (pl != null) LoadedPlugins[pl.Name] = pl;
                        PluginLoadingCompleted(plugin);
                    });
                }
            });
        }

        /// <summary>
        /// Called when a CompilablePlugin wants to be compiled
        /// </summary>
        /// <param name="plugin"></param>
        public void CompilationRequested(CompilablePlugin plugin)
        {
            if (Compilation.Current != null)
            {
                //Interface.Oxide.LogDebug("Adding plugin to outstanding compilation: {0}", plugin.Name);
                Compilation.Current.Add(plugin);
                return;
            }
            if (compilationQueue.Count < 1)
            {
                Interface.Oxide.NextTick(() =>
                {
                    CompileAssembly(compilationQueue.ToArray());
                    compilationQueue.Clear();
                });
            }
            compilationQueue.Add(plugin);
        }

        public void PluginLoadingStarted(CompilablePlugin plugin)
        {
            // Let the Oxide core know that this plugin will be loading asynchronously
            LoadingPlugins.Add(plugin.Name);
            plugin.IsLoading = true;
        }

        private void PluginLoadingCompleted(CompilablePlugin plugin)
        {
            LoadingPlugins.Remove(plugin.Name);
            plugin.IsLoading = false;
            foreach (var loadingName in LoadingPlugins.ToArray())
            {
                var loadingPlugin = GetCompilablePlugin(plugin.Directory, loadingName);
                if (loadingPlugin.IsLoading && loadingPlugin.Requires.Contains(plugin.Name))
                    Load(loadingPlugin);
            }
        }

        private void CompileAssembly(CompilablePlugin[] plugins)
        {
            compiler.Compile(plugins, compilation =>
            {
                if (compilation.compiledAssembly == null)
                {
                    foreach (var plugin in compilation.plugins)
                    {
                        plugin.OnCompilationFailed();
                        PluginErrors[plugin.Name] = $"Failed to compile: {plugin.CompilerErrors}";
                        Interface.Oxide.LogError($"Error while compiling: {plugin.CompilerErrors}");
                        //RemoteLogger.Warning($"{plugin.ScriptName} plugin failed to compile!\n{plugin.CompilerErrors}");
                    }
                }
                else
                {
                    if (compilation.plugins.Count > 0)
                    {
                        var compiledNames = compilation.plugins.Where(pl => string.IsNullOrEmpty(pl.CompilerErrors)).Select(pl => pl.Name).ToArray();
                        var verb = compiledNames.Length > 1 ? "were" : "was";
                        Interface.Oxide.LogInfo($"{compiledNames.ToSentence()} {verb} compiled successfully in {Math.Round(compilation.duration * 1000f)}ms");
                    }

                    foreach (var plugin in compilation.plugins)
                    {
                        if (plugin.CompilerErrors == null)
                        {
                            Interface.Oxide.UnloadPlugin(plugin.Name);
                            plugin.OnCompilationSucceeded(compilation.compiledAssembly);
                        }
                        else
                        {
                            plugin.OnCompilationFailed();
                            PluginErrors[plugin.Name] = $"Failed to compile: {plugin.CompilerErrors}";
                            Interface.Oxide.LogError($"Error while compiling: {plugin.CompilerErrors}");
                        }
                    }
                }
            });
        }

        public void OnShutdown() => compiler.Shutdown();
    }
}
