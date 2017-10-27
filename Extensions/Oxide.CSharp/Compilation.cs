using ObjectStream.Data;
using Oxide.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace Oxide.Plugins
{
    internal class Compilation
    {
        public static Compilation Current;

        internal int id;
        internal string name;
        internal Action<Compilation> callback;
        internal ConcurrentHashSet<CompilablePlugin> queuedPlugins;
        internal HashSet<CompilablePlugin> plugins = new HashSet<CompilablePlugin>();
        internal float startedAt;
        internal float endedAt;
        internal Hash<string, CompilerFile> references = new Hash<string, CompilerFile>();
        internal HashSet<string> referencedPlugins = new HashSet<string>();
        internal CompiledAssembly compiledAssembly;
        internal float duration => endedAt - startedAt;

        private string includePath;
        private string[] extensionNames;
        private string gameExtensionName;
        private string gameExtensionNamespace;

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

            includePath = Path.Combine(Interface.Oxide.PluginDirectory, "include");
            extensionNames = Interface.Oxide.GetAllExtensions().Select(ext => ext.Name).ToArray();
            var gameExtension = Interface.Oxide.GetAllExtensions().SingleOrDefault(ext => ext.IsGameExtension);
            gameExtensionName = gameExtension?.Name.ToUpper();
            gameExtensionNamespace = gameExtension?.GetType().Namespace;
        }

        internal void Started()
        {
            startedAt = Interface.Oxide.Now;
            name = (plugins.Count < 2 ? plugins.First().Name : "plugins_") + Math.Round(Interface.Oxide.Now * 10000000f) + ".dll";
        }

        internal void Completed(byte[] rawAssembly = null)
        {
            endedAt = Interface.Oxide.Now;
            if (plugins.Count > 0 && rawAssembly != null)
                compiledAssembly = new CompiledAssembly(name, plugins.ToArray(), rawAssembly, duration);
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
                var loadedPlugin = CSharpPluginLoader.GetCompilablePlugin(plugin.Directory, pl.Name);
                if (!loadedPlugin.Requires.Contains(plugin.Name)) continue;

                AddDependency(loadedPlugin);
            }
        }

        internal bool IncludesRequiredPlugin(string name)
        {
            if (referencedPlugins.Contains(name)) return true;

            var compilablePlugin = plugins.SingleOrDefault(pl => pl.Name == name);
            return compilablePlugin != null && compilablePlugin.CompilerErrors == null;
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
                    foreach (var filename in CSharpPluginLoader.PluginReferences)
                    {
                        if (File.Exists(Path.Combine(Interface.Oxide.ExtensionDirectory, filename + ".dll")))
                            references[filename + ".dll"] = new CompilerFile(Interface.Oxide.ExtensionDirectory, filename + ".dll");
                        if (File.Exists(Path.Combine(Interface.Oxide.ExtensionDirectory, filename + ".exe")))
                            references[filename + ".exe"] = new CompilerFile(Interface.Oxide.ExtensionDirectory, filename + ".exe");
                    }

                    //Interface.Oxide.LogDebug("Preparing compilation");

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
                            //Interface.Oxide.LogDebug("Adding plugin to compilation: " + plugin.Name);
                            PreparseScript(plugin);
                            ResolveReferences(plugin);
                        }
                        else
                        {
                            //Interface.Oxide.LogDebug("Plugin is already part of compilation: " + plugin.Name);
                        }

                        CacheModifiedScripts();

                        // We don't want the main thread to be able to add more plugins which could be missed
                        if (queuedPlugins.Count == 0 && Current == this)
                        {
                            Current = null;
                            //Interface.Oxide.LogDebug("Probably done preparing compilation: " + plugins.Select(p => p.Name).ToSentence());
                        }
                    }

                    //Interface.Oxide.LogDebug("Done preparing compilation: " + plugins.Select(p => p.Name).ToSentence());

                    callback();
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException("Exception while resolving plugin references", ex);
                    //RemoteLogger.Exception("Exception while resolving plugin references", ex);
                }
            });
        }

        private void PreparseScript(CompilablePlugin plugin)
        {
            plugin.References.Clear();
            plugin.IncludePaths.Clear();
            plugin.Requires.Clear();

            var parsingNamespace = false;
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

                    var className = match.Groups[1].Value;
                    if (className != plugin.Name)
                    {
                        Interface.Oxide.LogError($"Plugin filename {plugin.ScriptName}.cs must match the main class {className} (should be {className}.cs)");
                        plugin.CompilerErrors = $"Plugin filename {plugin.ScriptName}.cs must match the main class {className} (should be {className}.cs)";
                        RemovePlugin(plugin);
                    }

                    break;
                }

                // Include explicit plugin dependencies defined by magic comments in script
                match = Regex.Match(line, @"^//\s*Requires:\s*(\S+?)(\.cs)?\s*$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var dependencyName = match.Groups[1].Value;
                    plugin.Requires.Add(dependencyName);
                    if (!File.Exists(Path.Combine(plugin.Directory, dependencyName + ".cs")))
                    {
                        Interface.Oxide.LogError($"{plugin.Name} plugin requires missing dependency: {dependencyName}");
                        plugin.CompilerErrors = $"Missing dependency: {dependencyName}";
                        RemovePlugin(plugin);
                        return;
                    }

                    //Interface.Oxide.LogDebug(plugin.Name + " plugin requires dependency: " + dependency_name);
                    var dependencyPlugin = CSharpPluginLoader.GetCompilablePlugin(plugin.Directory, dependencyName);
                    AddDependency(dependencyPlugin);
                    continue;
                }

                // Include explicit references defined by magic comments in script
                match = Regex.Match(line, @"^//\s*Reference:\s*(\S+)\s*$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var result = match.Groups[1].Value;
                    // TODO: Fix Oxide.References to avoid these and other dependency conflicts
                    if (!result.StartsWith("Oxide.") && !result.StartsWith("Newtonsoft.Json") && !result.StartsWith("Rust.Workshop"))
                    {
                        AddReference(plugin, result);
                        Interface.Oxide.LogInfo("Added '// Reference: {0}' in plugin '{1}'", result, plugin.Name);
                    }
                    else
                        Interface.Oxide.LogWarning("Ignored unnecessary '// Reference: {0}' in plugin '{1}'", result, plugin.Name);
                    continue;
                }

                // Include implicit references detected from using statements in script
                match = Regex.Match(line, @"^\s*using\s+(Oxide\.(?:Core|Ext|Game)\.(?:[^\.]+))[^;]*;.*$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var result = match.Groups[1].Value;
                    var newResult = Regex.Replace(result, @"Oxide\.[\w]+\.([\w]+)", "Oxide.$1");
                    if (!string.IsNullOrEmpty(newResult) && File.Exists(Path.Combine(Interface.Oxide.ExtensionDirectory, newResult + ".dll")))
                        AddReference(plugin, newResult);
                    else
                        AddReference(plugin, result);
                    continue;
                }

                // Start parsing the Oxide.Plugins namespace contents
                match = Regex.Match(line, @"^\s*namespace Oxide\.Plugins\s*(\{\s*)?$", RegexOptions.IgnoreCase);
                if (match.Success) parsingNamespace = true;
            }
        }

        private void ResolveReferences(CompilablePlugin plugin)
        {
            foreach (var reference in plugin.References)
            {
                var match = Regex.Match(reference, @"^(Oxide\.(?:Ext|Game)\.(.+))$", RegexOptions.IgnoreCase);
                if (!match.Success) continue;

                var fullName = match.Groups[1].Value;
                var name = match.Groups[2].Value;
                if (extensionNames.Contains(name)) continue;

                if (Directory.Exists(includePath))
                {
                    var includeFilePath = Path.Combine(includePath, $"Ext.{name}.cs");
                    if (File.Exists(includeFilePath))
                    {
                        plugin.IncludePaths.Add(includeFilePath);
                        continue;
                    }
                }

                var message = $"{fullName} is referenced by {plugin.Name} plugin but is not loaded! An appropriate include file needs to be saved to plugins\\include\\Ext.{name}.cs if this extension is not required.";
                Interface.Oxide.LogError(message);
                plugin.CompilerErrors = message;
                RemovePlugin(plugin);
            }
        }

        private void AddDependency(CompilablePlugin plugin)
        {
            if (plugin.IsLoading || plugins.Contains(plugin) || queuedPlugins.Contains(plugin)) return;

            var compiledDependency = plugin.CompiledAssembly;
            if (compiledDependency != null && !compiledDependency.IsOutdated())
            {
                // The dependency already has a compiled assembly which is up to date
                referencedPlugins.Add(plugin.Name);
                if (!references.ContainsKey(compiledDependency.Name))
                    references[compiledDependency.Name] = new CompilerFile(compiledDependency.Name, compiledDependency.RawAssembly);
            }
            else
            {
                // The dependency needs to be compiled
                Add(plugin);
            }
        }

        private void AddReference(CompilablePlugin plugin, string assemblyName)
        {
            var path = Path.Combine(Interface.Oxide.ExtensionDirectory, assemblyName + ".dll");
            if (!File.Exists(path))
            {
                if (assemblyName.StartsWith("Oxide."))
                {
                    plugin.References.Add(assemblyName);
                    return;
                }

                Interface.Oxide.LogError($"Assembly referenced by {plugin.Name} plugin does not exist: {assemblyName}.dll");
                plugin.CompilerErrors = $"Referenced assembly does not exist: {assemblyName}";
                RemovePlugin(plugin);
                return;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException)
            {
                Interface.Oxide.LogError($"Assembly referenced by {plugin.Name} plugin is invalid: {assemblyName}.dll");
                plugin.CompilerErrors = $"Referenced assembly is invalid: {assemblyName}";
                RemovePlugin(plugin);
                return;
            }

            AddReference(plugin, assembly.GetName());

            // Include references made by the referenced assembly
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                // TODO: Fix Oxide.References to avoid these and other dependency conflicts
                if (reference.Name.StartsWith("Newtonsoft.Json") || reference.Name.StartsWith("Rust.Workshop")) continue;

                var referencePath = Path.Combine(Interface.Oxide.ExtensionDirectory, reference.Name + ".dll");
                if (!File.Exists(referencePath))
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
            var waitingForAccess = false;
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
                            while (!reader.EndOfStream) lines.Add(reader.ReadLine());
                            if (!string.IsNullOrEmpty(gameExtensionName)) lines.Insert(0, $"#define {gameExtensionName}");
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
                    if (!waitingForAccess)
                    {
                        waitingForAccess = true;
                        Interface.Oxide.LogWarning("Waiting for another application to stop using script: {0}", plugin.Name);
                    }
                    Thread.Sleep(50);
                }
            }
        }

        private void CacheModifiedScripts()
        {
            var modifiedPlugins = plugins.Where(pl => pl.ScriptLines == null || pl.HasBeenModified() || pl.LastCachedScriptAt != pl.LastModifiedAt).ToArray();
            if (modifiedPlugins.Length < 1) return;

            foreach (var plugin in modifiedPlugins) CacheScriptLines(plugin);
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
            foreach (var requiredPlugin in plugins.Where(pl => !pl.IsCompilationNeeded && plugin.Requires.Contains(pl.Name)).ToArray())
                if (!plugins.Any(pl => pl.Requires.Contains(requiredPlugin.Name))) RemovePlugin(requiredPlugin);
        }
    }
}
