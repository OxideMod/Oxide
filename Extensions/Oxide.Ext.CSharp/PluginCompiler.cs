using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using Mono.Unix.Native;
using ObjectStream;
using ObjectStream.Data;

using Oxide.Core;

namespace Oxide.Plugins
{
    public class PluginCompiler
    {
        public static bool AutoShutdown = true;
        public static string BinaryPath;
        public static string CompilerVersion;

        public static void CheckCompilerBinary()
        {
            BinaryPath = null;
            var rootDirectory = Interface.Oxide.RootDirectory;
            var binaryPath = rootDirectory + @"\basic.exe";
            if (File.Exists(binaryPath))
            {
                BinaryPath = binaryPath;
                return;
            }
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    binaryPath = rootDirectory + @"\CSharpCompiler.exe";
                    if (!File.Exists(binaryPath))
                    {
                        Interface.Oxide.LogError("Cannot compile C# (.cs) plugins. Unable to find CSharpCompiler.exe!");
                        return;
                    }
                    break;
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    binaryPath = rootDirectory + @"/CSharpCompiler";
                    if (IntPtr.Size != 8) binaryPath += ".x86";
                    if (!File.Exists(binaryPath))
                    {
                        Interface.Oxide.LogError("Cannot compile C# (.cs) plugins. Unable to find CSharpCompiler!");
                        return;
                    }
                    Syscall.chmod(binaryPath, FilePermissions.S_IRWXU | FilePermissions.S_IRGRP | FilePermissions.S_IXGRP | FilePermissions.S_IROTH| FilePermissions.S_IXOTH);
                    break;
            }
            BinaryPath = EscapePath(binaryPath);

            var versionInfo = FileVersionInfo.GetVersionInfo(binaryPath);
            CompilerVersion = $"{versionInfo.FileMajorPart}.{versionInfo.FileMinorPart}.{versionInfo.FileBuildPart}.{versionInfo.FilePrivatePart}";
            RemoteLogger.SetTag("compiler version", CompilerVersion);
        }

        private Process process;
        private readonly Regex fileErrorRegex = new Regex(@"([\w\.]+)\(\d+\,\d+\+?\): error|error \w+: Source file `[\\\./]*([\w\.]+)", RegexOptions.Compiled);
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
            var endedProcess = process;
            if (endedProcess != null) endedProcess.Exited -= OnProcessExited;
            process = null;
            if (client == null) return;
            client.Message -= OnMessage;
            client.Error -= OnError;
            client.PushMessage(new CompilerMessage { Type = CompilerMessageType.Exit });
            client.Stop();
            client = null;
            if (endedProcess == null) return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5000);
                // Calling Close can block up to 60 seconds on certain machines
                if (!endedProcess.HasExited) endedProcess.Close();
            });
        }

        private void EnqueueCompilation(Compilation compilation)
        {
            if (compilation.plugins.Count < 1)
            {
                //Interface.Oxide.LogDebug("EnqueueCompilation called for an empty compilation");
                return;
            }
            if (!CheckCompiler())
            {
                OnCompilerFailed($"Compiler v{CompilerVersion} couldn't be started.");
                return;
            }
            compilation.Started();
            //Interface.Oxide.LogDebug("Compiling with references: {0}", compilation.references.Keys.ToSentence());
            var sourceFiles = compilation.plugins.SelectMany(plugin => plugin.IncludePaths).Distinct().Select(path => new CompilerFile(path)).ToList();
            sourceFiles.AddRange(compilation.plugins.Select(plugin => new CompilerFile($"{plugin.ScriptName}.cs", plugin.ScriptSource)));
            //Interface.Oxide.LogDebug("Compiling files: {0}", sourceFiles.Select(f => f.Name).ToSentence());
            var data = new CompilerData
            {
                OutputFile = compilation.name,
                SourceFiles = sourceFiles.ToArray(),
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
                    OnCompilerFailed($"Compiler v{CompilerVersion} disconnected."); // TODO: Expand, warn about possible missing depdencies
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
                        Interface.Oxide.LogWarning("Compiler compiled an unknown assembly!"); // TODO: Clarify which assembly
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
                                var fileName = value.Basename();
                                var scriptName = fileName.Substring(0, fileName.Length - 3);
                                var compilablePlugin = compilation.plugins.SingleOrDefault(pl => pl.ScriptName == scriptName);
                                if (compilablePlugin == null)
                                {
                                    Interface.Oxide.LogError("Unable to resolve script error to plugin: {0}", line);
                                    continue;
                                }
                                var missingRequirements = compilablePlugin.Requires.Where(name => !compilation.IncludesRequiredPlugin(name));
                                if (missingRequirements.Any())
                                    compilablePlugin.CompilerErrors = $"Missing dependencies: {missingRequirements.ToSentence()}";
                                else
                                    compilablePlugin.CompilerErrors = line.Trim().Replace(Interface.Oxide.PluginDirectory + Path.DirectorySeparatorChar, string.Empty);
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
                        while (messageQueue.Count > 0) connection.PushMessage(messageQueue.Dequeue());
                    }
                    break;
            }
        }

        private static void OnError(Exception exception) => Interface.Oxide.LogException("Compilation error: ", exception);

        private bool CheckCompiler()
        {
            CheckCompilerBinary();
            idleTimer?.Destroy();
            if (BinaryPath == null) return false;
            if (process != null && process.Handle != IntPtr.Zero && !process.HasExited) return true;
            PurgeOldLogs();
            Shutdown();
            var args = new[] { "/service", "/logPath:" + EscapePath(Interface.Oxide.LogDirectory) };
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
                switch (Environment.OSVersion.Platform)
                {
                    case PlatformID.Win32S:
                    case PlatformID.Win32Windows:
                    case PlatformID.Win32NT:
                        process.StartInfo.EnvironmentVariables["PATH"] = $"{Path.Combine(Interface.Oxide.ExtensionDirectory, "x86")}";
                        break;
                    case PlatformID.Unix:
                    case PlatformID.MacOSX:
                        process.StartInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = $"{Path.Combine(Interface.Oxide.ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86")}";
                        break;
                }
                process.Exited += OnProcessExited;
                process.Start();
            }
            catch (Exception ex)
            {
                process?.Dispose();
                process = null;
                Interface.Oxide.LogException($"Exception while starting compiler v{CompilerVersion}: ", ex); // TODO: Expand, warn that it may not be executable
                if (ex.GetBaseException() != ex) Interface.Oxide.LogException("BaseException: ", ex.GetBaseException());
                var win32 = ex as Win32Exception;
                if (win32 != null) Interface.Oxide.LogError("Win32 NativeErrorCode: {0} ErrorCode: {1} HelpLink: {2}", win32.NativeErrorCode, win32.ErrorCode, win32.HelpLink);
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
                OnCompilerFailed($"Compiler v{CompilerVersion} closed."); // TODO: Expand, warn about possible security software?
                Shutdown();
            });
        }

        private void OnCompilerFailed(string reason)
        {
            foreach (var compilation in compilations.Values)
            {
                foreach (var plugin in compilation.plugins) plugin.CompilerErrors = reason;
                compilation.Completed();
            }
            compilations.Clear();
        }

        private static void PurgeOldLogs()
        {
            try
            {
                var filePaths = Directory.GetFiles(Interface.Oxide.LogDirectory, "*.txt").Where(f =>
                {
                    var fileName = Path.GetFileName(f);
                    return fileName != null && fileName.StartsWith("compiler_");
                });
                foreach (var filePath in filePaths) File.Delete(filePath);
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        private static string EscapePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "\"\"";
            path = Regex.Replace(path, @"(\\*)" + "\"", @"$1\$0");
            path = Regex.Replace(path, @"^(.*\s.*?)(\\*)$", "\"$1$2$2\"");
            return path;
        }
    }
}
