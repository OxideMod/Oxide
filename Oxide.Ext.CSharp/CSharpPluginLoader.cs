using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class CSharpPluginLoader : PluginLoader
    {
        public static string[] DefaultReferences = { "mscorlib", "System", "System.Core", "System.Data", "Oxide.Core", "Oxide.Ext.CSharp" };
        public static HashSet<string> PluginReferences = new HashSet<string>(DefaultReferences);

        private CSharpExtension extension;
        private Dictionary<string, CompilablePlugin> plugins = new Dictionary<string, CompilablePlugin>();
        private List<CompilablePlugin> compilationQueue = new List<CompilablePlugin>();

        public CSharpPluginLoader(CSharpExtension extension)
        {
            this.extension = extension;

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

            var compilable_plugin = GetCompilablePlugin(extension, directory, name);
            compilable_plugin.Compile(compiled =>
            {
                // Load the plugin assembly if it was successfully compiled
                if (compiled)
                    compilable_plugin.LoadPlugin(plugin =>
                    {
                        LoadingPlugins.Remove(name);
                        if (plugin != null)
                        {
                            plugin.Loader = this;
                            LoadedPlugins[compilable_plugin.Name] = plugin;
                        }
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
            var compilable_plugin = GetCompilablePlugin(extension, directory, name);
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
        /// <param name="plugin"></param>
        public override void Unloading(Plugin plugin_base)
        {
            var plugin = plugin_base as CSharpPlugin;
            LoadedPlugins.Remove(plugin.Name);
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
                var plugin_names = compiler.Plugins.Select(p => p.Name).ToSentence();
                if (compiler.Plugins.Count > 1 && raw_assembly == null)
                {
                    Interface.Oxide.LogError($"A batch of {compiler.Plugins.Count} plugins failed to compile, attempting to compile separately");
                    foreach (var plugin in compiler.Plugins) CompileAssembly(new[] { plugin });
                    return;
                }
                if (raw_assembly == null)
                {
                    var plugin = compiler.Plugins[0];
                    plugin.OnCompilationFailed();
                    PluginErrors[plugin.Name] = "Failed to compile";
                    Interface.Oxide.LogError("{0} plugin failed to compile! Exit code: {1}", plugin.ScriptName, compiler.ExitCode);
                    var debug_output = $"{plugin.ScriptName} plugin failed to compile! Exit code: {compiler.ExitCode}";
                    foreach (var raw_line in compiler.StdOutput.ToString().Split('\n'))
                    {
                        var line = raw_line.Trim();
                        if (line.Length < 1 || line.StartsWith("Compilation failed: ")) continue;
                        Interface.Oxide.LogWarning(line);
                        debug_output += "\n" + line;
                    }
                    if (compiler.ErrOutput.Length > 0)
                    {
                        var error_output = compiler.ErrOutput.ToString();
                        error_output = error_output.Replace(Interface.Oxide.PluginDirectory + "\\", string.Empty);
                        PluginErrors[plugin.Name] = "Failed to compile: " + error_output;
                        Interface.Oxide.LogError(error_output);
                        debug_output += "\n" + error_output;
                    }
                    RemoteLogger.Warning(debug_output);
                }
                else
                {
                    var compiled_plugins = compiler.Plugins.Where(pl => pl.CompilerErrors == null).ToArray();
                    CompiledAssembly compiled_assembly = null;
                    if (compiled_plugins.Length > 0)
                    {
                        var compiled_names = compiled_plugins.Select(pl => pl.Name).ToSentence();
                        var verb = compiled_names.Length > 1 ? "were" : "was";
                        Interface.Oxide.LogInfo($"{compiled_names} {verb} compiled successfully in {Math.Round(compiler.Duration * 1000f)}ms");
                        compiled_assembly = new CompiledAssembly(compiled_plugins, raw_assembly);
                    }
                    foreach (var plugin in compiler.Plugins)
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
