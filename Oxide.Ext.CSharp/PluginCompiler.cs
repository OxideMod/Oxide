using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Diagnostics;

using Oxide.Core;

namespace Oxide.Plugins
{
    public class PluginCompiler
    {
        public static string BinaryPath;

        public string StdOutput;
        public string ErrOutput;
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

            string options = "/t:library";

            // Include references made by the CSharpPlugins project
            foreach (var reference_name in CSharpPluginLoader.ProjectReferences)
                if (reference_name != "mscorlib" && reference_name != "System" && !reference_name.StartsWith("System."))
                    options = string.Format("{0} /r:{1}\\RustDedicated_Data\\Managed\\{2}.dll", options, Directory.GetCurrentDirectory(), reference_name);

            // Include references defined by magic comments in script
            foreach (var line in File.ReadAllLines(plugin.ScriptPath))
            {
                var match = Regex.Match(line, @"^//\s?Reference:\s?(\S+)$", RegexOptions.IgnoreCase);
                if (!match.Success) break;

                var path = string.Format("{0}\\RustDedicated_Data\\Managed\\{1}.dll", Directory.GetCurrentDirectory(), match.Groups[1].Value);
                if (!File.Exists(path))
                {
                    Interface.GetMod().LogInfo("Assembly referenced by {0} plugin does not exist: {1}.dll", plugin.Name, match.Groups[1].Value);
                    continue;
                }

                options = string.Format("{0} /r:{1}", options, path);
            }

            var arguments = string.Format("{0} /out:{1}\\{2}_{3}.dll {4}", options, Interface.GetMod().TempDirectory, plugin.Name, plugin.CompilationCount, plugin.ScriptPath);
            var start_info = new ProcessStartInfo(BinaryPath, arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process = new Process { StartInfo = start_info, EnableRaisingEvents = true };

            startedAt = UnityEngine.Time.realtimeSinceStartup;
            StdOutput = "";
            ErrOutput = "";

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

        public float Duration
        {
            get { return endedAt - startedAt; }
        }

        private void OnStdOutput(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            StdOutput += e.Data;
        }

        private void OnErrorOutput(object sender, DataReceivedEventArgs e)
        {
            var process = sender as Process;
            ErrOutput += e.Data;
        }

        private void OnExited(object sender, EventArgs e)
        {
            endedAt = UnityEngine.Time.realtimeSinceStartup;
            ExitCode = process.ExitCode;
            Interface.GetMod().NextTick(() => callback(ExitCode == 0));
        }
    }
}
