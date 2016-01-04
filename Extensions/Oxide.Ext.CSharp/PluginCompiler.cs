using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                    if (!File.Exists(root_directory + @"\mono-2.0.dll"))
                    {
                        Interface.Oxide.LogError("Cannot compile C# plugins. Unable to find mono-2.0.dll!");
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
        private Hash<int, Compilation> compilations;
        private Queue<CompilerMessage> messageQueue;
        private volatile int lastId;
        private volatile bool ready;
        private Core.Libraries.Timer.TimerInstance idleTimer;

        public PluginCompiler()
        {
            compilations = new Hash<int, Compilation>();
            messageQueue = new Queue<CompilerMessage>();
        }

        internal void Compile(CompilablePlugin[] plugins, Action<Compilation> callback)
        {
            var id = lastId++;
            var compilation = new Compilation(id, callback, plugins);
            compilations[id] = compilation;
            compilation.Prepare(() => EnqueueCompilation(compilation));
        }

        public void Shutdown()
        {
            ready = false;
            var ended_process = process;
            if (ended_process != null) ended_process.Exited -= OnProcessExited;
            process = null;
            if (client == null) return;
            client.Message -= OnMessage;
            client.Error -= OnError;
            client.PushMessage(new CompilerMessage { Type = CompilerMessageType.Exit });
            client.Stop();
            client = null;
            if (ended_process == null) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5000);
                // Calling Close can block up to 60 seconds on certain machines
                if (!ended_process.HasExited) ended_process.Close();
            });
        }

        private void EnqueueCompilation(Compilation compilation)
        {
            if (compilation.plugins.Count < 1)
            {
                Interface.Oxide.LogDebug("EnqueueCompilation called for an empty compilation");
                return;
            }
            if (!CheckCompiler())
            {
                OnCompilerFailed("Compiler couldn't be started.");
                return;
            }
            compilation.Started();
            //Interface.Oxide.LogDebug("Compiling with references: " + compilation.references.Keys.ToSentence());
            var source_files = compilation.plugins.SelectMany(plugin => plugin.IncludePaths).Distinct().Select(path => new CompilerFile(path)).ToList();
            source_files.AddRange(compilation.plugins.Select(plugin => new CompilerFile(plugin.ScriptName + ".cs", plugin.ScriptSource)));
            //Interface.Oxide.LogDebug("Compiling files: " + source_files.Select(f => f.Name).ToSentence());
            var data = new CompilerData
            {
                OutputFile = compilation.name,
                SourceFiles = source_files.ToArray(),
                ReferenceFiles = compilation.references.Values.ToArray()
            };
            var message = new CompilerMessage { Id = compilation.id, Data = data, Type = CompilerMessageType.Compile };
            if (ready)
                client.PushMessage(message);
            else
                messageQueue.Enqueue(message);
        }

        private void OnMessage(ObjectStreamConnection<CompilerMessage, CompilerMessage> connection, CompilerMessage message)
        {
            if (message == null)
            {
                Interface.Oxide.NextTick(() =>
                {
                    OnCompilerFailed("Compiler disconnected.");
                    Shutdown();
                });
                return;
            }
            switch (message.Type)
            {
                case CompilerMessageType.Assembly:
                    var compilation = compilations[message.Id];
                    if (compilation == null)
                    {
                        Interface.Oxide.LogWarning("CSharp compiler compiled an unknown assembly!");
                        return;
                    }
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
                                    compilable_plugin.CompilerErrors = $"Missing dependencies: {missing_requirements.ToSentence()}";
                                else
                                    compilable_plugin.CompilerErrors = line.Trim().Replace(Interface.Oxide.PluginDirectory + Path.DirectorySeparatorChar, string.Empty);
                            }
                        }
                    }
                    compilation.Completed((byte[])message.Data);
                    compilations.Remove(message.Id);
                    idleTimer?.Destroy();
                    if (AutoShutdown)
                    {
                        Interface.Oxide.NextTick(() =>
                        {
                            idleTimer?.Destroy();
                            if (AutoShutdown) idleTimer = Interface.Oxide.GetLibrary<Core.Libraries.Timer>().Once(60, Shutdown);
                        });
                    }
                    break;
                case CompilerMessageType.Error:
                    Interface.Oxide.LogError("Compilation error: {0}", message.Data);
                    compilations[message.Id].Completed();
                    compilations.Remove(message.Id);
                    idleTimer?.Destroy();
                    if (AutoShutdown)
                    {
                        Interface.Oxide.NextTick(() =>
                        {
                            idleTimer?.Destroy();
                            idleTimer = Interface.Oxide.GetLibrary<Core.Libraries.Timer>().Once(60, Shutdown);
                        });
                    }
                    break;
                case CompilerMessageType.Ready:
                    connection.PushMessage(message);
                    if (!ready)
                    {
                        ready = true;
                        while (messageQueue.Count > 0)
                            connection.PushMessage(messageQueue.Dequeue());
                    }
                    break;
            }
        }

        private void OnError(Exception exception)
        {
            Interface.Oxide.LogException("Compilation error: ", exception);
        }

        private bool CheckCompiler()
        {
            CheckCompilerBinary();
            idleTimer?.Destroy();
            if (BinaryPath == null) return false;
            if (process != null && !process.HasExited) return true;
            PurgeOldLogs();
            Shutdown();
            var args = new [] {"/service", "/logPath:" + EscapeArgument(Interface.Oxide.LogDirectory)};
            try
            {
                process = new Process
                {
                    StartInfo =
                    {
                        FileName = BinaryPath,
                        Arguments = string.Join(" ", args),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };
                process.Exited += OnProcessExited;
                process.Start();
            }
            catch (Exception ex)
            {
                process?.Dispose();
                process = null;
                Interface.Oxide.LogException("Exception while starting compiler: ", ex);
                if (ex.GetBaseException() != ex) Interface.Oxide.LogException("BaseException: ", ex.GetBaseException());
            }
            if (process == null) return false;
            client = new ObjectStreamClient<CompilerMessage>(process.StandardOutput.BaseStream, process.StandardInput.BaseStream);
            client.Message += OnMessage;
            client.Error += OnError;
            client.Start();
            return true;
        }

        private void OnProcessExited(object sender, EventArgs eventArgs)
        {
            Interface.Oxide.NextTick(() =>
            {
                OnCompilerFailed("Compiler closed.");
                Shutdown();
            });
        }

        private void OnCompilerFailed(string reason)
        {
            foreach (var compilation in compilations.Values)
            {
                foreach (var plugin in compilation.plugins)
                {
                    plugin.CompilerErrors = reason;
                }
                compilation.Completed();
            }
            compilations.Clear();
        }

        private void PurgeOldLogs()
        {
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

        private static string EscapeArgument(string arg)
        {
            if (string.IsNullOrEmpty(arg)) return "\"\"";
            arg = Regex.Replace(arg, @"(\\*)" + "\"", @"$1\$0");
            arg = Regex.Replace(arg, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
            return arg;
        }
    }
}
