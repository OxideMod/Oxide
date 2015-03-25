using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Reflection;

using Oxide.Core;

namespace Oxide.Plugins
{
    public class PluginCompiler
    {
        public static string BinaryPath;

        public List<CompilablePlugin> Plugins;
        public StringBuilder StdOutput;
        public StringBuilder ErrOutput;
        public int ExitCode;
        public float Duration => endedAt - startedAt;

        private Action<byte[]> callback;
        private Process process;
        private float startedAt;
        private float endedAt;
        private string compiledName;
        private HashSet<string> references;

        public PluginCompiler(CompilablePlugin[] plugins)
        {
            Plugins = new List<CompilablePlugin>(plugins);
        }

        public PluginCompiler(CompilablePlugin plugin)
        {
            Plugins = new List<CompilablePlugin> { plugin };
        }

        public void ResolveReferences(Action callback)
        {
            // Include references made by the CSharpPlugins project
            references = new HashSet<string>(CSharpPluginLoader.PluginReferences);
            
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    CacheAllScripts();

                    foreach (var plugin in Plugins.ToArray())
                    {
                        // Include references defined by magic comments in script
                        foreach (var line in plugin.ScriptLines)
                        {
                            var match = Regex.Match(line, @"^//\s?Reference:\s?(\S+)$", RegexOptions.IgnoreCase);
                            if (!match.Success) break;

                            var assembly_name = match.Groups[1].Value;

                            var path = string.Format("{0}\\{1}.dll", Interface.Oxide.ExtensionDirectory, assembly_name);
                            if (!File.Exists(path))
                            {
                                Interface.Oxide.LogError("Assembly referenced by {0} plugin does not exist: {1}.dll", plugin.Name, assembly_name);
                                plugin.CompilerErrors = "Referenced assembly does not exist: " + assembly_name;
                                RemovePlugin(plugin);
                                continue;
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
                                RemovePlugin(plugin);
                                continue;
                            }

                            references.Add(assembly_name);

                            // Include references made by the referenced assembly
                            foreach (var reference in assembly.GetReferencedAssemblies())
                                references.Add(reference.Name);
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

        public void Compile(Action<byte[]> callback)
        {
            if (BinaryPath == null) return;

            this.callback = callback;

            ResolveReferences(() =>
            {
                if (Plugins.Count < 1) return;
                foreach (var plugin in Plugins) plugin.CompilerErrors = null;
                compiledName = (Plugins.Count == 1 ? Plugins[0].Name : "plugins_") + Math.Round(Interface.Oxide.Now * 100000f);                
                SpawnCompiler();
            });
        }
        
        private void SpawnCompiler()
        {
            var arguments = new List<string> { "/sdk:2", "/t:library", "/langversion:6", "/noconfig", "/nostdlib+", "/platform:x64" };

            foreach (var reference_name in references)
                arguments.Add(string.Format("/r:{0}\\{1}.dll", Interface.Oxide.ExtensionDirectory, reference_name));

            arguments.Add(string.Format("/out:{0}\\{1}.dll", Interface.Oxide.TempDirectory, compiledName));
            arguments.AddRange(Plugins.Select(plugin => plugin.ScriptPath));

            var start_info = new ProcessStartInfo(BinaryPath, EscapeArguments(arguments.ToArray()))
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process = new Process { StartInfo = start_info, EnableRaisingEvents = true };

            startedAt = Interface.Oxide.Now;
            StdOutput = new StringBuilder();
            ErrOutput = new StringBuilder();

            process.OutputDataReceived += OnStdOutput;
            process.ErrorDataReceived += OnErrorOutput;
            process.Exited += OnExited;

            Interface.Oxide.LogDebug("Spawning compiler: " + Plugins.Select(pl => pl.Name).ToSentence());

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            ThreadPool.QueueUserWorkItem((_) =>
            {
                Thread.Sleep(120000);
                if (!process.HasExited)
                {
                    Interface.Oxide.NextTick(() =>
                    {
                        if (process.HasExited) return;
                        var plugin_names = Plugins.Select(p => p.Name).ToSentence();
                        Interface.Oxide.LogError("Timed out waiting for compiler to compile: " + plugin_names);
                        RemoteLogger.Error("Timed out waiting for compiler to compile: " + plugin_names);
                        process.Kill();
                    });
                }
            });
        }

        Regex fileErrorRegex = new Regex(@"([\w\.]+)\(\d+,\d+\): error|error \w+: Source file `[\\\./]*([\w\.]+)", RegexOptions.Compiled);

        private void OnStdOutput(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            if (e.Data.StartsWith("warning: restarting compilation"))
            {
                foreach (var line in StdOutput.ToString().Split('\n'))
                { 
                    var match = fileErrorRegex.Match(line.Trim());
                    for (var i = 1; i < match.Groups.Count; i++)
                    {
                        var value = match.Groups[i].Value;
                        if (value == null || value.Trim() == string.Empty) continue;
                        var file_name = value.Basename();
                        var script_name = file_name.Substring(0, file_name.Length - 3);
                        var compilable_plugin = Plugins.SingleOrDefault(pl => pl.ScriptName == script_name);
                        if (compilable_plugin == null)
                            Interface.Oxide.LogError("Unable to resolve script error to plugin: " + line);
                        else
                            compilable_plugin.CompilerErrors = line.Trim().Replace(Interface.Oxide.PluginDirectory + "\\", string.Empty);
                    }
                }
                StdOutput.Remove(0, StdOutput.Length);
                ErrOutput.Remove(0, ErrOutput.Length);
                return;
            }
            StdOutput.Append(e.Data);
        }

        private void OnErrorOutput(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            ErrOutput.Append(e.Data);
        }

        private void OnExited(object sender, EventArgs e)
        {
            endedAt = Interface.Oxide.Now;
            ExitCode = process.ExitCode;
            byte[] raw_assembly = null;
            if (ExitCode == 0)
            {
                try
                {
                    var assembly_path = string.Format("{0}\\{1}.dll", Interface.Oxide.TempDirectory, compiledName);
                    raw_assembly = File.ReadAllBytes(assembly_path);
                    File.Delete(assembly_path);
                }
                catch (Exception ex)
                {
                    var plugin_names = Plugins.Select(p => p.Name).ToSentence();
                    Interface.Oxide.LogError("Unable to read compiled plugins: {0} ({1})", plugin_names, ex.Message);
                    RemoteLogger.Error($"Unable to read compiled plugins: {plugin_names} ({ex.Message})");
                }
            }
            Interface.Oxide.NextTick(() => callback(raw_assembly));
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
                    plugin.ScriptLines = File.ReadAllLines(plugin.ScriptPath);
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
            Thread.Sleep(100);
            var modified_plugins = Plugins.Where(pl => pl.HasBeenModified()).ToArray();
            if (modified_plugins.Length < 1) return;
            foreach (var plugin in modified_plugins)
                CacheScriptLines(plugin);
            CacheModifiedScripts();
        }

        private void CacheAllScripts()
        {
            foreach (var plugin in Plugins.ToArray())
                if (!CacheScriptLines(plugin)) RemovePlugin(plugin);
            CacheModifiedScripts();
        }
        
        private void RemovePlugin(CompilablePlugin plugin)
        {
            Plugins.Remove(plugin);
            plugin.OnCompilationFailed();
        }

        /// <summary>
        /// Quotes all arguments that contain whitespace, or begin with a quote and returns a single argument string
        /// </summary>
        /// <param name="args">A list of strings for arguments, may not contain null, '\0', '\r', or '\n'</param>
        /// <returns>A string containing the combined list of escaped/quoted strings</returns>
        /// <exception cref="System.ArgumentNullException">Raised when one of the arguments is null</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Raised if an argument contains '\0', '\r', or '\n'</exception>
        private string EscapeArguments(params string[] args)
        {
            var arguments = new StringBuilder();
            var invalid_char = new Regex("[\x00\x0a\x0d]");  // these can not be escaped
            var needs_quotes = new Regex(@"\s|""");          // contains whitespace or two quote characters
            var escape_quote = new Regex(@"(\\*)(""|$)");    // one or more '\' followed with a quote or end of string
            for (int i = 0; args != null && i < args.Length; i++)
            {
                if (args[i] == null) throw new ArgumentNullException("args[" + i + "]");
                if (invalid_char.IsMatch(args[i])) throw new ArgumentOutOfRangeException("args[" + i + "]");
                if (args[i] == String.Empty)
                    arguments.Append("\"\"");
                else if (!needs_quotes.IsMatch(args[i]))
                    arguments.Append(args[i]);
                else
                {
                    arguments.Append('"');
                    arguments.Append(escape_quote.Replace(args[i], m =>
                        m.Groups[1].Value + m.Groups[1].Value + (m.Groups[2].Value == "\"" ? "\\\"" : ""))
                    );
                    arguments.Append('"');
                }
                if (i + 1 < args.Length) arguments.Append(' ');
            }
            return arguments.ToString();
        }
    }
}
