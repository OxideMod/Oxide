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

        public StringBuilder StdOutput;
        public StringBuilder ErrOutput;
        public int ExitCode;

        private CompilablePlugin plugin;
        private Action<bool> callback;
        private Process process;
        private float startedAt;
        private float endedAt;
        private bool waitingForAccess;

        public PluginCompiler(CompilablePlugin plugin)
        {
            this.plugin = plugin;
        }

        public void Compile(Action<bool> callback)
        {
            if (BinaryPath == null) return;

            if (!File.Exists(plugin.ScriptPath))
            {
                Interface.GetMod().LogInfo("Unable to compile plugin. File not found: {0}", plugin.ScriptPath);
                callback(false);
                return;
            }

            this.callback = callback;

            plugin.OnCompilerStarted();

            BuildReferences();
        }

        public float Duration
        {
            get { return endedAt - startedAt; }
        }

        private void BuildReferences()
        {
            string[] source_lines;

            try
            {
                source_lines = File.ReadAllLines(plugin.ScriptPath);
                waitingForAccess = false;
            }
            catch (IOException ex)
            {
                if (!waitingForAccess)
                {
                    waitingForAccess = true;
                    Interface.GetMod().LogInfo("Waiting for another application to stop using script: {0}", plugin.Name);
                }                
                Interface.GetMod().NextTick(BuildReferences);
                return;
            }

            // Include references made by the CSharpPlugins project
            var references = new HashSet<string>(CSharpPluginLoader.ProjectReferences);

            // Include references defined by magic comments in script
            foreach (var line in source_lines)
            {
                var match = Regex.Match(line, @"^//\s?Reference:\s?(\S+)$", RegexOptions.IgnoreCase);
                if (!match.Success) break;

                var assembly_name = match.Groups[1].Value;

                var path = string.Format("{0}\\{1}.dll", Interface.GetMod().ExtensionDirectory, assembly_name);
                if (!File.Exists(path))
                {
                    Interface.GetMod().LogInfo("Assembly referenced by {0} plugin does not exist: {1}.dll", plugin.Name, assembly_name);
                    continue;
                }

                references.Add(assembly_name);

                // Include references made by the referenced assembly
                foreach (var reference in Assembly.Load(assembly_name).GetReferencedAssemblies())
                    references.Add(reference.Name);
            }

            SpawnCompiler(references);
        }

        private void SpawnCompiler(HashSet<string> references)
        {
            var arguments = new List<string> { "/sdk:2", "/t:library", "/langversion:6", "/noconfig", "/nostdlib+", "/platform:x64" };

            foreach (var reference_name in references)
                //if (reference_name != "mscorlib" && reference_name != "System")
                    arguments.Add(string.Format("/r:{0}\\{1}.dll", Interface.GetMod().ExtensionDirectory, reference_name));

            arguments.Add(string.Format("/out:{0}\\{1}_{2}.dll", Interface.GetMod().TempDirectory, plugin.Name, plugin.CompilationCount));
            arguments.Add(plugin.ScriptPath);

            var start_info = new ProcessStartInfo(BinaryPath, EscapeArguments(arguments.ToArray()))
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process = new Process { StartInfo = start_info, EnableRaisingEvents = true };

            startedAt = UnityEngine.Time.realtimeSinceStartup;
            StdOutput = new StringBuilder();
            ErrOutput = new StringBuilder();

            process.OutputDataReceived += OnStdOutput;
            process.ErrorDataReceived += OnErrorOutput;
            process.Exited += OnExited;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            ThreadPool.QueueUserWorkItem((_) =>
            {
                Thread.Sleep(30000);
                if (!process.HasExited)
                {
                    Interface.GetMod().NextTick(() =>
                    {
                        Interface.GetMod().LogInfo("ERROR: Timed out waiting for compiler!");
                        process.Kill();
                    });
                }
            });
        }

        private void OnStdOutput(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            StdOutput.Append(e.Data);
        }

        private void OnErrorOutput(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            ErrOutput.Append(e.Data);
        }

        private void OnExited(object sender, EventArgs e)
        {
            endedAt = UnityEngine.Time.realtimeSinceStartup;
            ExitCode = process.ExitCode;
            Interface.GetMod().NextTick(() => callback(ExitCode == 0));
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
