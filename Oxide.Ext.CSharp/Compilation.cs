using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using Oxide.Core;

using ObjectStream.Data;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Oxide.Plugins
{
    internal class Compilation
    {
        public static Compilation Current;

        internal int id;
        internal string name;
        internal Action<Compilation> callback;
        internal ConcurrentHashSet<CompilablePlugin> queuedPlugins = new ConcurrentHashSet<CompilablePlugin>();
        internal HashSet<CompilablePlugin> plugins = new HashSet<CompilablePlugin>();
        internal float startedAt;
        internal float endedAt;
        internal Hash<string, CompilerFile> references = new Hash<string, CompilerFile>();
        internal HashSet<string> referencedPlugins = new HashSet<string>();
        internal CompiledAssembly compiledAssembly;
        internal float duration => endedAt - startedAt;

        private string includePath;
        private string[] extensionNames;
        private string gameExtensionNamespace;
        private bool newGameExtensionNamespace;

        internal Compilation(int id, Action<Compilation> callback, CompilablePlugin[] plugins)
        {
            this.id = id;
            this.callback = callback;
            this.queuedPlugins = new ConcurrentHashSet<CompilablePlugin>(plugins);

            if (Current == null) Current = this;

            foreach (var plugin in plugins)
            {
                plugin.CompilerErrors = null;
                plugin.OnCompilationStarted();
            }

            includePath = Path.Combine(Interface.Oxide.PluginDirectory, "Include");
            extensionNames = Interface.Oxide.GetAllExtensions().Select(ext => ext.Name).ToArray();
            gameExtensionNamespace = Interface.Oxide.GetAllExtensions().SingleOrDefault(ext => ext.IsGameExtension)?.GetType().Namespace;
            newGameExtensionNamespace = gameExtensionNamespace != null && gameExtensionNamespace.Contains(".Game.");
        }

        internal void Started()
        {
            startedAt = Interface.Oxide.Now;
            name = (plugins.Count < 2 ? plugins.First().Name : "plugins_") + Math.Round(Interface.Oxide.Now * 10000000f) + ".dll";
        }

        internal void Completed(byte[] raw_assembly = null)
        {
            endedAt = Interface.Oxide.Now;
            if (plugins.Count > 0 && raw_assembly != null)
                compiledAssembly = new CompiledAssembly(name, plugins.ToArray(), raw_assembly, duration);
            Interface.Oxide.NextTick(() => callback(this));
        }

        internal void Add(CompilablePlugin plugin)
        {
            if (!queuedPlugins.Add(plugin)) return;
            plugin.Loader.PluginLoadingStarted(plugin);
            plugin.CompilerErrors = null;
            plugin.OnCompilationStarted();
            foreach (var pl in Interface.Oxide.RootPluginManager.GetPlugins().Where(pl => pl is CSharpPlugin))
            {
                var loaded_plugin = CSharpPluginLoader.GetCompilablePlugin(plugin.Directory, pl.Name);
                if (!loaded_plugin.Requires.Contains(plugin.Name)) continue;
                AddDependency(loaded_plugin);
            }
        }

        internal bool IncludesRequiredPlugin(string name)
        {
            if (referencedPlugins.Contains(name)) return true;
            var compilable_plugin = plugins.SingleOrDefault(pl => pl.Name == name);
            return compilable_plugin != null && compilable_plugin.CompilerErrors == null;
        }

        internal void Prepare(Action callback)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    referencedPlugins.Clear();
                    references.Clear();

                    // Include references made by the CSharpPlugins project
                    foreach (var name in CSharpPluginLoader.PluginReferences)
                        references[name + ".dll"] = new CompilerFile(Interface.Oxide.ExtensionDirectory, name + ".dll");
                                        
                    CompilablePlugin plugin;
                    while (queuedPlugins.TryDequeue(out plugin))
                    {
                        if (Current == null) Current = this;

                        if (!CacheScriptLines(plugin) || plugin.ScriptLines.Length < 1)
                        {
                            plugin.References.Clear();
                            plugin.IncludePaths.Clear();
                            plugin.Requires.Clear();
                            Interface.Oxide.LogWarning("Plugin script is empty: " + plugin.Name);
                            RemovePlugin(plugin);

                        }
                        else if (plugins.Add(plugin))
                        {
                            PreparseScript(plugin);
                            ResolveReferences(plugin);
                        }

                        CacheModifiedScripts();

                        // We don't want the main thread to be able to add more plugins which could be missed
                        if (queuedPlugins.Count == 0 && Current == this) { Current = null; }
                    }

                    //Interface.Oxide.LogDebug("Done preparing compilation: " + plugins.Select(p => p.Name).ToSentence());

                    callback();
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException("Exception while resolving plugin references", ex);
                    RemoteLogger.Exception("Exception while resolving plugin references", ex);
                }
            });
        }

        private void PreparseScript(CompilablePlugin plugin)
        {
            plugin.References.Clear();
            plugin.IncludePaths.Clear();
            plugin.Requires.Clear();
            
            bool parsingNamespace = false;
            for (var i = 0; i < plugin.ScriptLines.Length; i++)
            {
                var line = plugin.ScriptLines[i].Trim();
                if (line.Length < 1) continue;

                Match match;
                if (parsingNamespace)
                {
                    // Skip blank lines and opening brace at the top of the namespace block
                    match = Regex.Match(line, @"^\s*\{?\s*$", RegexOptions.IgnoreCase);
                    if (match.Success) continue;

                    // Skip class custom attributes
                    match = Regex.Match(line, @"^\s*\[", RegexOptions.IgnoreCase);
                    if (match.Success) continue;

                    // Detect main plugin class name
                    match = Regex.Match(line, @"^\s*(?:public|private|protected|internal)?\s*class\s+(\S+)\s+\:\s+\S+Plugin\s*$", RegexOptions.IgnoreCase);
                    if (!match.Success) break;

                    var class_name = match.Groups[1].Value;
                    if (class_name != plugin.Name)
                    {
                        Interface.Oxide.LogError($"Plugin filename is incorrect: {plugin.ScriptName}.cs (should be {class_name}.cs)");
                        plugin.CompilerErrors = $"Filename is incorrect: {plugin.ScriptName}.cs (should be {class_name}.cs)";
                        RemovePlugin(plugin);
                    }

                    break;
                }
                else
                {
                    // Include explicit plugin dependencies defined by magic comments in script
                    match = Regex.Match(line, @"^//\s*Requires:\s*(\S+?)(\.cs)?\s*$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var dependency_name = match.Groups[1].Value;
                        plugin.Requires.Add(dependency_name);
                        if (!File.Exists(Path.Combine(plugin.Directory, dependency_name + ".cs")))
                        {
                            Interface.Oxide.LogError($"{plugin.Name} plugin requires missing dependency: {dependency_name}");
                            plugin.CompilerErrors = $"Missing dependency: {dependency_name}";
                            RemovePlugin(plugin);
                            return;
                        }
                        Interface.Oxide.LogDebug(plugin.Name + " plugin requires dependency: " + dependency_name);
                        var dependency_plugin = CSharpPluginLoader.GetCompilablePlugin(plugin.Directory, dependency_name);
                        AddDependency(dependency_plugin);
                        continue;
                    }

                    // Include explicit references defined by magic comments in script
                    match = Regex.Match(line, @"^//\s*Reference:\s*(\S+)\s*$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var result = match.Groups[1].Value;
                        if (!newGameExtensionNamespace || !result.StartsWith(gameExtensionNamespace.Replace(".Game.", ".Ext.")))
                            AddReference(plugin, match.Groups[1].Value);
                        else
                            Interface.Oxide.LogWarning("Ignored obsolete game extension reference '{0}' in plugin '{1}'", result, plugin.Name);
                        continue;
                    }

                    // Include implicit references detected from using statements in script
                    match = Regex.Match(line, @"^\s*using\s+(Oxide\.(?:Ext|Game)\.(?:[^\.]+))[^;]*;\s*$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        AddReference(plugin, match.Groups[1].Value);
                        continue;
                    }

                    // Start parsing the Oxide.Plugins namespace contents
                    match = Regex.Match(line, @"^\s*namespace Oxide\.Plugins\s*(\{\s*)?$", RegexOptions.IgnoreCase);
                    if (match.Success) parsingNamespace = true;
                }
            }
        }

        private void ResolveReferences(CompilablePlugin plugin)
        {
            foreach (var reference in plugin.References)
            {
                var match = Regex.Match(reference, @"^(Oxide\.(?:Ext|Game)\.(.+))$", RegexOptions.IgnoreCase);
                if (!match.Success) continue;
                var full_name = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                if (extensionNames.Contains(name)) continue;
                if (Directory.Exists(includePath))
                {
                    var include_file_path = Path.Combine(includePath, $"Ext.{name}.cs");
                    if (File.Exists(include_file_path))
                    {
                        plugin.IncludePaths.Add(include_file_path);
                        continue;
                    }
                }
                var message = $"{full_name} is referenced by {plugin.Name} plugin but is not loaded! An appropriate include file needs to be saved to Plugins\\Include\\Ext.{name}.cs if this extension is not required.";
                Interface.Oxide.LogError(message);
                plugin.CompilerErrors = message;
                RemovePlugin(plugin);
            }
        }

        private void AddDependency(CompilablePlugin plugin)
        {
            if (plugin.IsLoading || plugins.Contains(plugin) || queuedPlugins.Contains(plugin)) return;
            var compiled_dependency = plugin.CompiledAssembly;
            if (compiled_dependency != null && !compiled_dependency.IsOutdated())
            {
                // The dependency already has a compiled assembly which is up to date
                referencedPlugins.Add(plugin.Name);
                if (!references.ContainsKey(compiled_dependency.Name))
                    references[compiled_dependency.Name] = new CompilerFile(compiled_dependency.Name, compiled_dependency.RawAssembly);
            }
            else
            {
                // The dependency needs to be compiled
                Add(plugin);
            }
        }

        private void AddReference(CompilablePlugin plugin, string assembly_name)
        {
            var path = Path.Combine(Interface.Oxide.ExtensionDirectory, assembly_name + ".dll");
            if (!File.Exists(path))
            {
                if (assembly_name.StartsWith("Oxide.Ext."))
                {
                    plugin.References.Add(assembly_name);
                    return;
                }
                Interface.Oxide.LogError($"Assembly referenced by {plugin.Name} plugin does not exist: {assembly_name}.dll");
                plugin.CompilerErrors = "Referenced assembly does not exist: " + assembly_name;
                RemovePlugin(plugin);
                return;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.Load(assembly_name);
            }
            catch (FileNotFoundException)
            {
                Interface.Oxide.LogError($"Assembly referenced by {plugin.Name} plugin is invalid: {assembly_name}.dll");
                plugin.CompilerErrors = "Referenced assembly is invalid: " + assembly_name;
                RemovePlugin(plugin);
                return;
            }

            AddReference(plugin, assembly.GetName());

            // Include references made by the referenced assembly
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var reference_path = Path.Combine(Interface.Oxide.ExtensionDirectory, reference.Name + ".dll");
                if (!File.Exists(reference_path))
                {
                    Interface.Oxide.LogWarning($"Reference {reference.Name}.dll from {assembly.GetName().Name}.dll not found");
                    continue;
                }
                AddReference(plugin, reference);
            }
        }

        private void AddReference(CompilablePlugin plugin, AssemblyName reference)
        {
            var filename = reference.Name + ".dll";
            if (!references.ContainsKey(filename)) references[filename] = new CompilerFile(Interface.Oxide.ExtensionDirectory, filename);
            plugin.References.Add(reference.Name);
        }
        
        private bool CacheScriptLines(CompilablePlugin plugin)
        {
            var waiting_for_access = false;
            while (true)
            {
                try
                {
                    if (!File.Exists(plugin.ScriptPath))
                    {
                        Interface.Oxide.LogWarning("Script no longer exists: {0}", plugin.Name);
                        plugin.CompilerErrors = "Plugin file was deleted";
                        RemovePlugin(plugin);
                        return false;
                    }
                    plugin.CheckLastModificationTime();
                    if (plugin.LastCachedScriptAt != plugin.LastModifiedAt)
                    {
                        using (var reader = File.OpenText(plugin.ScriptPath))
                        {
                            var lines = new List<string>();
                            while (!reader.EndOfStream)
                                lines.Add(reader.ReadLine());
                            plugin.ScriptLines = lines.ToArray();
                            plugin.ScriptEncoding = reader.CurrentEncoding;
                        }
                        plugin.LastCachedScriptAt = plugin.LastModifiedAt;
                        if (plugins.Remove(plugin))
                            queuedPlugins.Add(plugin);
                    }
                    return true;
                }
                catch (IOException)
                {
                    if (!waiting_for_access)
                    {
                        waiting_for_access = true;
                        Interface.Oxide.LogWarning("Waiting for another application to stop using script: {0}", plugin.Name);
                    }
                    Thread.Sleep(50);
                }
            }
        }

        private void CacheModifiedScripts()
        {
            var modified_plugins = plugins.Where(pl => pl.ScriptLines == null || pl.HasBeenModified() || pl.LastCachedScriptAt != pl.LastModifiedAt).ToArray();
            if (modified_plugins.Length < 1) return;
            foreach (var plugin in modified_plugins)
                CacheScriptLines(plugin);
            Thread.Sleep(100);
            CacheModifiedScripts();
        }

        private void RemovePlugin(CompilablePlugin plugin)
        {
            if (plugin.LastCompiledAt == default(DateTime)) return;
            queuedPlugins.Remove(plugin);
            plugins.Remove(plugin);
            plugin.OnCompilationFailed();
            // Remove plugins which are required by this plugin if they are only being compiled for this requirement
            foreach (var required_plugin in plugins.Where(pl => !pl.IsCompilationNeeded && plugin.Requires.Contains(pl.Name)).ToArray())
            {
                if (!plugins.Any(pl => pl.Requires.Contains(required_plugin.Name))) RemovePlugin(required_plugin);
            }
        }
    }
}
