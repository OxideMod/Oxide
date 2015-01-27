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
            }
            catch (IOException ex)
            {
                Interface.GetMod().LogInfo("IOException while reading {0} script: {1}", plugin.Name, ex.Message);
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

                var path = string.Format("{0}\\RustDedicated_Data\\Managed\\{1}.dll", Directory.GetCurrentDirectory(), assembly_name);
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
            var arguments = new StringBuilder("/t:library");

            foreach (var reference_name in references)
                if (reference_name != "mscorlib" && reference_name != "System" && !reference_name.StartsWith("System."))
                    arguments.AppendFormat(@" /r:{0}\RustDedicated_Data\Managed\{1}.dll", Directory.GetCurrentDirectory(), reference_name);
            
            arguments.AppendFormat(" /out:{0}\\{1}_{2}.dll {3}", Interface.GetMod().TempDirectory, plugin.Name, plugin.CompilationCount, plugin.ScriptPath);

            var start_info = new ProcessStartInfo(BinaryPath, arguments.ToString())
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process = new Process { StartInfo = start_info, EnableRaisingEvents = true };

            startedAt = UnityEngine.Time.realtimeSinceStartup;
            StdOutput = new StringBuilder("");
            ErrOutput = new StringBuilder("");

            process.OutputDataReceived += OnStdOutput;
            process.ErrorDataReceived += OnErrorOutput;
            process.Exited += OnExited;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            ThreadPool.QueueUserWorkItem((_) =>
            {
                Thread.Sleep(5000);
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
    }
}
