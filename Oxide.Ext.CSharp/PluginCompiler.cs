using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

using ObjectStream;
using ObjectStream.Data;

using Oxide.Core;

namespace Oxide.Plugins
{
    public class PluginCompiler
    {
        public static bool AutoShutdown = true;
        public static string BinaryPath;

        public static void CheckCompilerBinary()
        {
            BinaryPath = null;
            var root_directory = Interface.Oxide.RootDirectory;
            var binary_path = root_directory + @"\basic.exe";
            if (File.Exists(binary_path))
            {
                BinaryPath = binary_path;
                return;
            }
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    if (!File.Exists(root_directory + @"\monosgen-2.0.dll"))
                    {
                        Interface.Oxide.LogError("Cannot compile C# plugins. Unable to find monosgen-2.0.dll!");
                        return;
                    }
                    if (!File.Exists(root_directory + @"\msvcr120.dll") && !File.Exists(Environment.SystemDirectory + @"\msvcr120.dll"))
                    {
                        Interface.Oxide.LogError("Cannot compile C# plugins. Unable to find msvcr120.dll!");
                        return;
                    }
                    binary_path = root_directory + @"\CSharpCompiler.exe";
                    if (!File.Exists(binary_path))
                    {
                        Interface.Oxide.LogError("Cannot compile C# plugins. Unable to find CSharpCompiler.exe!");
                        return;
                    }
                    break;
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    binary_path = root_directory + @"/CSharpCompiler";
                    if (!File.Exists(binary_path))
                    {
                        Interface.Oxide.LogError("Cannot compile C# plugins. Unable to find CSharpCompiler!");
                        return;
                    }
                    break;
            }
            BinaryPath = binary_path;
        }

        private Process process;
        private Regex fileErrorRegex = new Regex(@"([\w\.]+)\(\d+,\d+\): error|error \w+: Source file `[\\\./]*([\w\.]+)", RegexOptions.Compiled);
        private ObjectStreamClient<CompilerMessage> client;
        private Dictionary<int, Compilation> pluginComp;
        private Queue<CompilerMessage> compQueue;
        private volatile int lastId;
        private volatile bool ready;
        private Core.Libraries.Timer.TimerInstance idleTimer;

        class Compilation
        {
            public string name;
            public Action<string, byte[], float> callback;
            public List<CompilablePlugin> plugins;
            public float startedAt;
            public float endedAt;
            public HashSet<CompilerFile> references;
            public HashSet<string> referencedPlugins = new HashSet<string>();
            public float Duration => endedAt - startedAt;

            public void Started()
            {
                startedAt = Interface.Oxide.Now;
                name = (plugins.Count == 1 ? plugins[0].Name : "plugins_") + Math.Round(Interface.Oxide.Now * 10000000f) + ".dll";
            }

            public void Completed(byte[] raw_assembly = null)
            {
                endedAt = Interface.Oxide.Now;
                Interface.Oxide.NextTick(() => callback(name, raw_assembly, Duration));
            }

            public bool IncludesRequiredPlugin(string name)
            {
                if (referencedPlugins.Contains(name)) return true;
                var compilable_plugin = plugins.SingleOrDefault(pl => pl.Name == name);
                return compilable_plugin != null && compilable_plugin.CompilerErrors == null;
            }
        }

        public PluginCompiler()
        {
            pluginComp = new Dictionary<int, Compilation>();
            compQueue = new Queue<CompilerMessage>();
        }

        public void ResolveReferences(int currentId, Action callback)
        {
            var compilation = pluginComp[currentId];

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    compilation.referencedPlugins.Clear();

                    // Include references made by the CSharpPlugins project
                    compilation.references = new HashSet<CompilerFile>(
                        CSharpPluginLoader.PluginReferences.Select(name => new CompilerFile(Interface.Oxide.ExtensionDirectory, name + ".dll"))
                    );

                    CacheAllScripts(compilation.plugins);

                    var extension_names = Interface.Oxide.GetAllExtensions().Select(ext => ext.Name).ToArray();
                    var game_extension_ns = Interface.Oxide.GetAllExtensions().SingleOrDefault(ext => ext.IsGameExtension)?.GetType().Namespace;
                    var new_game_ext = game_extension_ns != null && game_extension_ns.Contains(".Game.");
                    var include_path = Path.Combine(Interface.Oxide.PluginDirectory, "Include");

                    foreach (var plugin in compilation.plugins.ToArray())
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
                                    Interface.Oxide.LogError("Plugin filename is incorrect: {0}.cs (should be {1}.cs)", plugin.ScriptName, class_name);
                                    plugin.CompilerErrors = string.Format("Plugin filename is incorrect: {0}.cs (should be {1}.cs)", plugin.ScriptName, class_name);
                                    RemovePlugin(compilation.plugins, plugin);
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
                                    var directory = compilation.plugins[0].Directory;
                                    if (!File.Exists($"{directory}{Path.DirectorySeparatorChar}{dependency_name}.cs"))
                                    {
                                        var message = $"{plugin.Name} plugin requires missing dependency: {dependency_name}";
                                        Interface.Oxide.LogError(message);
                                        plugin.CompilerErrors = message;
                                        RemovePlugin(compilation.plugins, plugin);
                                        break;
                                    }
                                    Interface.Oxide.LogDebug(plugin.Name + " plugin requires dependency: " + dependency_name);
                                    if (!compilation.plugins.Any(pl => pl.Name == dependency_name))
                                    {
                                        // The dependency is not currently a plugin which is added to this compilation
                                        var dependency_plugin = CSharpPluginLoader.GetCompilablePlugin(directory, dependency_name);
                                        if (dependency_plugin.IsReloading)
                                        {
                                            Interface.Oxide.LogDebug("Dependency is already reloading: " + dependency_name);
                                            continue;
                                        }
                                        if (dependency_plugin.CompiledAssembly != null && !dependency_plugin.CompiledAssembly.CompilablePlugins.Any(pl => pl.IsCompiledAssemblyOutdated()))
                                        {
                                            // The dependency already has a compiled assembly which is up to date
                                            compilation.referencedPlugins.Add(dependency_plugin.Name);
                                            var compiled_dependency = dependency_plugin.CompiledAssembly;
                                            if (compilation.references.Any(r => r.Name == compiled_dependency.Name))
                                            {
                                                Interface.Oxide.LogDebug($"Dependency is already compiled: {dependency_name} (already referenced)");
                                            }
                                            else
                                            {
                                                Interface.Oxide.LogDebug($"Dependency is already compiled: {dependency_name} (adding as reference)");
                                                compilation.references.Add(new CompilerFile(compiled_dependency.Name, compiled_dependency.RawAssembly));
                                            }
                                        }
                                        else
                                        {
                                            // The dependency needs to be compiled
                                            dependency_plugin.IsReloading = true;
                                            dependency_plugin.OnCompilationStarted(this);
                                            dependency_plugin.Compile((compiled) =>
                                            {
                                                if (!compiled)
                                                {
                                                    Interface.Oxide.LogError("Plugin failed to compile: {0} (leaving previous version loaded)", dependency_name);
                                                    dependency_plugin.IsReloading = false;
                                                    return;
                                                }
                                                Interface.Oxide.UnloadPlugin(dependency_name);
                                                // Delay load by a frame so that all plugins in a batch can be unloaded before the assembly is loaded
                                                Interface.Oxide.NextTick(() =>
                                                {
                                                    if (!Interface.Oxide.LoadPlugin(dependency_name)) dependency_plugin.IsReloading = false;
                                                });
                                            }, false);
                                            compilation.plugins.Insert(0, dependency_plugin);
                                        }
                                    }
                                    continue;
                                }

                                // Include explicit references defined by magic comments in script
                                match = Regex.Match(line, @"^//\s*Reference:\s*(\S+)\s*$", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    var result = match.Groups[1].Value;
                                    //TODO temp ignore renamed game exts
                                    if (!new_game_ext || !result.StartsWith(game_extension_ns.Replace(".Game.", ".Ext.")))
                                        AddReference(currentId, plugin, match.Groups[1].Value);
                                    else
                                        Interface.Oxide.LogWarning("Ignored obsolete game extension reference '{0}' in plugin '{1}'", result, plugin.Name);
                                    continue;
                                }

                                // Include implicit references detected from using statements in script
                                match = Regex.Match(line, @"^\s*using\s+(Oxide\.(?:Ext|Game)\.(?:[^\.]+))[^;]*;\s*$", RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    AddReference(currentId, plugin, match.Groups[1].Value);
                                    continue;
                                }

                                // Start parsing the Oxide.Plugins namespace contents
                                match = Regex.Match(line, @"^\s*namespace Oxide\.Plugins\s*(\{\s*)?$", RegexOptions.IgnoreCase);
                                if (match.Success) parsingNamespace = true;
                            }
                        }

                        foreach (var reference in plugin.References)
                        {
                            var match = Regex.Match(reference, @"^(Oxide\.(?:Ext|Game)\.(.+))$", RegexOptions.IgnoreCase);
                            if (!match.Success) continue;
                            var full_name = match.Groups[1].Value;
                            var name = match.Groups[2].Value;
                            if (extension_names.Contains(name)) continue;
                            if (Directory.Exists(include_path))
                            {
                                var include_file_path = Path.Combine(include_path, "Ext." + name + ".cs");
                                if (File.Exists(include_file_path))
                                {
                                    plugin.IncludePaths.Add(include_file_path);
                                    continue;
                                }
                            }
                            var message = $"{full_name} is referenced by {plugin.Name} plugin but is not loaded! An appropriate include file needs to be saved to Plugins\\Include\\Ext.{name}.cs if this extension is not required.";
                            Interface.Oxide.LogError(message);
                            plugin.CompilerErrors = message;
                            RemovePlugin(compilation.plugins, plugin);
                        }
                    }

                    callback();
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException("Exception while resolving plugin references", ex);
                    RemoteLogger.Exception("Exception while resolving plugin references", ex);
                }
            });
        }

        private void AddReference(int currentId, CompilablePlugin plugin, string assembly_name)
        {
            var compilation = pluginComp[currentId];
            var path = Path.Combine(Interface.Oxide.ExtensionDirectory, string.Format("{0}.dll", assembly_name));
            if (!File.Exists(path))
            {
                if (assembly_name.StartsWith("Oxide.Ext."))
                {
                    plugin.References.Add(assembly_name);
                    return;
                }
                Interface.Oxide.LogError("Assembly referenced by {0} plugin does not exist: {1}.dll", plugin.Name, assembly_name);
                plugin.CompilerErrors = "Referenced assembly does not exist: " + assembly_name;
                RemovePlugin(compilation.plugins, plugin);
                return;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.Load(assembly_name);
            }
            catch (FileNotFoundException)
            {
                Interface.Oxide.LogError("Assembly referenced by {0} plugin is invalid: {1}.dll", plugin.Name, assembly_name);
                plugin.CompilerErrors = "Referenced assembly is invalid: " + assembly_name;
                RemovePlugin(compilation.plugins, plugin);
                return;
            }

            compilation.references.Add(new CompilerFile(Interface.Oxide.ExtensionDirectory, assembly_name + ".dll"));
            plugin.References.Add(assembly_name);

            // Include references made by the referenced assembly
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                var reference_path = Path.Combine(Interface.Oxide.ExtensionDirectory, string.Format("{0}.dll", reference.Name));
                if (!File.Exists(reference_path))
                {
                    Interface.Oxide.LogWarning("Reference {0}.dll from {1}.dll not found.", reference.Name, assembly.GetName().Name);
                    continue;
                }
                compilation.references.Add(new CompilerFile(Interface.Oxide.ExtensionDirectory, reference.Name + ".dll"));
                plugin.References.Add(reference.Name);
            }
        }

        public void Compile(List<CompilablePlugin> plugins, Action<string, byte[], float> callback)
        {
            var currentId = lastId++;
            pluginComp[currentId] = new Compilation { callback = callback, plugins = plugins };
            if (!CheckCompiler())
            {
                foreach (var compilation in pluginComp.Values)
                {
                    foreach (var plugin in compilation.plugins)
                    {
                        plugin.CompilerErrors = "Compiler couldn't be started.";
                    }
                    compilation.Completed();
                }
                pluginComp.Clear();
                return;
            }

            ResolveReferences(currentId, () =>
            {
                if (plugins.Count < 1) return;
                foreach (var plugin in plugins) plugin.CompilerErrors = null;
                SpawnCompiler(currentId);
            });
        }

        private void SpawnCompiler(int currentId)
        {
            var compilation = pluginComp[currentId];
            compilation.Started();
            var source_files = compilation.plugins.SelectMany(plugin => plugin.IncludePaths).Distinct().Select(path => new CompilerFile(path)).ToList();
            source_files.AddRange(compilation.plugins.Select(plugin => new CompilerFile(plugin.ScriptName + ".cs", plugin.ScriptSource)));
            var compilerData = new CompilerData
            {
                OutputFile = compilation.name,
                SourceFiles = source_files.ToArray(),
                ReferenceFiles = compilation.references.ToArray()
            };
            var message = new CompilerMessage { Id = currentId, Data = compilerData, Type = CompilerMessageType.Compile };
            if (ready)
                client.PushMessage(message);
            else
                compQueue.Enqueue(message);
        }

        private void OnError(Exception exception)
        {
            Interface.Oxide.LogException("Compilation error: ", exception);
        }

        private void OnMessage(ObjectStreamConnection<CompilerMessage, CompilerMessage> connection, CompilerMessage message)
        {
            if (message == null)
            {
                Interface.Oxide.NextTick(OnShutdown);
                return;
            }
            switch (message.Type)
            {
                case CompilerMessageType.Assembly:
                    var compilation = pluginComp[message.Id];
                    compilation.endedAt = Interface.Oxide.Now;
                    var stdOutput = (string)message.ExtraData;
                    if (stdOutput != null)
                    {
                        foreach (var line in stdOutput.Split('\r', '\n'))
                        {
                            var match = fileErrorRegex.Match(line.Trim());
                            for (var i = 1; i < match.Groups.Count; i++)
                            {
                                var value = match.Groups[i].Value;
                                if (value.Trim() == string.Empty) continue;
                                var file_name = value.Basename();
                                var script_name = file_name.Substring(0, file_name.Length - 3);
                                var compilable_plugin = compilation.plugins.SingleOrDefault(pl => pl.ScriptName == script_name);
                                if (compilable_plugin == null)
                                {
                                    Interface.Oxide.LogError("Unable to resolve script error to plugin: " + line);
                                    continue;
                                }
                                var missing_requirements = compilable_plugin.Requires.Where(name => !compilation.IncludesRequiredPlugin(name));
                                if (missing_requirements.Any())
                                    compilable_plugin.CompilerErrors = $"{compilable_plugin.ScriptName}'s dependencies: {missing_requirements.ToSentence()}";
                                else
                                    compilable_plugin.CompilerErrors = line.Trim().Replace(Interface.Oxide.PluginDirectory + Path.DirectorySeparatorChar, string.Empty);
                            }
                        }
                    }
                    compilation.Completed((byte[])message.Data);
                    pluginComp.Remove(message.Id);
                    idleTimer?.Destroy();
                    if (AutoShutdown) idleTimer = Interface.Oxide.GetLibrary<Core.Libraries.Timer>().Once(60, OnShutdown);
                    break;
                case CompilerMessageType.Error:
                    Interface.Oxide.LogError("Compilation error: {0}", message.Data);
                    pluginComp[message.Id].Completed();
                    pluginComp.Remove(message.Id);
                    idleTimer?.Destroy();
                    if (AutoShutdown) idleTimer = Interface.Oxide.GetLibrary<Core.Libraries.Timer>().Once(60, OnShutdown);
                    break;
                case CompilerMessageType.Ready:
                    connection.PushMessage(message);
                    if (!ready)
                    {
                        ready = true;
                        while (compQueue.Count > 0)
                            connection.PushMessage(compQueue.Dequeue());
                    }
                    break;
            }
        }

        private void PurgeOldLogs()
        {
            //clear old logs
            try
            {
                var filePaths = Directory.GetFiles(Interface.Oxide.LogDirectory, "*.txt").Where(f =>
                {
                    var fileName = Path.GetFileName(f);
                    return fileName != null && fileName.StartsWith("compiler_");
                });
                foreach (var filePath in filePaths)
                    File.Delete(filePath);
            }
            catch (Exception) { }
        }

        private bool CheckCompiler()
        {
            CheckCompilerBinary();
            idleTimer?.Destroy();
            if (BinaryPath == null) return false;
            if (process != null && !process.HasExited) return true;
            PurgeOldLogs();
            OnShutdown();
            var args = new [] {"/service", "/logPath:" + EscapeArgument(Interface.Oxide.LogDirectory)};
            try
            {
                process = Process.Start(new ProcessStartInfo
                {
                    FileName = BinaryPath,
                    Arguments = string.Join(" ", args),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                });
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("Exception while starting compiler: ", ex);
            }
            if (process == null) return false;
            client = new ObjectStreamClient<CompilerMessage>(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
            client.Message += OnMessage;
            client.Error += OnError;
            client.Start();
            return true;
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
                        return false;
                    }
                    using (var reader = File.OpenText(plugin.ScriptPath))
                    {
                        var list = new List<string>();
                        while (!reader.EndOfStream)
                            list.Add(reader.ReadLine());
                        plugin.ScriptLines = list.ToArray();
                        plugin.ScriptEncoding = reader.CurrentEncoding;
                    }
                    //plugin.ScriptLines = File.ReadAllLines(plugin.ScriptPath);
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

        private void CacheModifiedScripts(List<CompilablePlugin> plugins)
        {
            Thread.Sleep(100);
            var modified_plugins = plugins.Where(pl => pl.HasBeenModified()).ToArray();
            if (modified_plugins.Length < 1) return;
            foreach (var plugin in modified_plugins)
                CacheScriptLines(plugin);
            CacheModifiedScripts(plugins);
        }

        private void CacheAllScripts(List<CompilablePlugin> plugins)
        {
            foreach (var plugin in plugins.ToArray())
                if (!CacheScriptLines(plugin)) RemovePlugin(plugins, plugin);
            CacheModifiedScripts(plugins);
        }

        private void RemovePlugin(List<CompilablePlugin> plugins, CompilablePlugin plugin)
        {
            if (!plugins.Remove(plugin)) return;
            plugin.OnCompilationFailed();
            // Remove plugins which are required by this plugin if they are only being compiled for this requirement
            foreach (var required_plugin in plugins.Where(pl => !pl.IsCompilationNeeded && plugin.Requires.Contains(pl.Name)).ToArray())
            {
                if (!plugins.Any(pl => pl.Requires.Contains(required_plugin.Name))) RemovePlugin(plugins, required_plugin);
            }
        }

        public void OnShutdown()
        {
            ready = false;
            var ended_process = process;
            process = null;
            if (client == null) return;
            client.PushMessage(new CompilerMessage { Type = CompilerMessageType.Exit });
            client.Stop();
            client = null;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5000);
                // Calling Close can block up to 60 seconds on certain machines
                if (!ended_process.HasExited) ended_process.Close();
            });
        }

        private static string EscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";
            arg = Regex.Replace(arg, @"(\\*)" + "\"", @"$1\$0");
            arg = Regex.Replace(arg, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
            return arg;
        }
    }
}
