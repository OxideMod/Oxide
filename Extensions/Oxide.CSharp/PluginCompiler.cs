extern alias Oxide;

using ObjectStream;
using ObjectStream.Data;
using Oxide.Core;
using Oxide::Mono.Unix.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;

namespace Oxide.Plugins
{
    public class PluginCompiler
    {
        public static bool AutoShutdown = true;
        public static bool TraceRan;
        public static string FileName = "basic.exe";
        public static string BinaryPath;
        public static string CompilerVersion;

        private static int downloadRetries = 0;

        public static void CheckCompilerBinary()
        {
            BinaryPath = null;
            var rootDirectory = Interface.Oxide.RootDirectory;
            var binaryPath = Path.Combine(rootDirectory, FileName);
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
                    FileName = "CSharpCompiler.exe";
                    binaryPath = Path.Combine(rootDirectory, FileName);
                    UpdateCheck(); // TODO: Only check once on server startup
                    break;

                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    FileName = $"CSharpCompiler.{(IntPtr.Size != 8 ? "x86" : "x86_x64")}";
                    binaryPath = Path.Combine(rootDirectory, FileName);
                    UpdateCheck(); // TODO: Only check once on server startup
                    try
                    {
                        if (Syscall.access(binaryPath, AccessModes.X_OK) == 0) break;
                    }
                    catch (Exception ex)
                    {
                        Interface.Oxide.LogError($"Unable to check {FileName} for executable permission");
                        Interface.Oxide.LogError(ex.Message);
                        Interface.Oxide.LogError(ex.StackTrace);
                    }
                    try
                    {
                        Syscall.chmod(binaryPath, FilePermissions.S_IRWXU);
                    }
                    catch (Exception ex)
                    {
                        Interface.Oxide.LogError($"Could not set {FileName} as executable, please set manually");
                        Interface.Oxide.LogError(ex.Message);
                        Interface.Oxide.LogError(ex.StackTrace);
                    }
                    break;
            }
            BinaryPath = binaryPath;
        }

        private void DependencyTrace()
        {
            if (TraceRan || Environment.OSVersion.Platform != PlatformID.Unix) return;

            try
            {
                Interface.Oxide.LogWarning($"Running dependency trace for {FileName}");
                var trace = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = Interface.Oxide.RootDirectory,
                        FileName = "/bin/bash",
                        Arguments = $"-c \"LD_TRACE_LOADED_OBJECTS=1 {BinaryPath}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true
                    },
                    EnableRaisingEvents = true
                };
                trace.StartInfo.EnvironmentVariables["LD_LIBRARY_PATH"] = $"{Path.Combine(Interface.Oxide.ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86")}";
                trace.ErrorDataReceived += (s, e) => Interface.Oxide.LogError(e.Data.TrimStart());
                trace.OutputDataReceived += (s, e) => Interface.Oxide.LogError(e.Data.TrimStart());
                trace.Start();
                trace.BeginOutputReadLine();
                trace.BeginErrorReadLine();
                trace.WaitForExit();
            }
            catch (Exception)
            {
                //Interface.Oxide.LogError($"Couldn't run dependency trace"); // TODO: Fix this triggering sometimes
                //Interface.Oxide.LogError(ex.Message);
            }
            TraceRan = true;
        }

        private static void DownloadCompiler(WebResponse response, string remoteHash)
        {
            try
            {
                Interface.Oxide.LogInfo($"Downloading {FileName} for .cs (C#) plugin compilation");

                var stream = response.GetResponseStream();
                var fs = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.None);
                var bufferSize = 10000;
                var buffer = new byte[bufferSize];

                while (true)
                {
                    var result = stream.Read(buffer, 0, bufferSize);
                    if (result == -1 || result == 0) break;
                    fs.Write(buffer, 0, result);
                }
                fs.Flush();
                fs.Close();
                stream.Close();
                response.Close();

                if (downloadRetries >= 3)
                {
                    Interface.Oxide.LogInfo($"Couldn't download {FileName}! Please download manually from: https://github.com/OxideMod/CSharpCompiler/releases/download/latest/{FileName}");
                    return;
                }

                var localHash = File.Exists(BinaryPath) ? GetHash(BinaryPath, Algorithms.MD5) : "0";
                if (remoteHash != localHash)
                {
                    Interface.Oxide.LogInfo($"Local hash did not match remote hash for {FileName}, attempting download again");
                    CheckCompilerBinary();
                    downloadRetries++;
                    return;
                }

                Interface.Oxide.LogInfo($"Download of {FileName} completed successfully");
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogError($"Couldn't download {FileName}! Please download manually from: https://github.com/OxideMod/CSharpCompiler/releases/download/latest/{FileName}");
                Interface.Oxide.LogError(ex.Message);
            }
        }

        private static void UpdateCheck()
        {
            try
            {
                var filePath = Path.Combine(Interface.Oxide.RootDirectory, FileName);
                var request = (HttpWebRequest)WebRequest.Create($"https://github.com/OxideMod/CSharpCompiler/releases/download/latest/{FileName}");
                var response = (HttpWebResponse)request.GetResponse();
                var statusCode = (int)response.StatusCode;
                if (statusCode != 200) Interface.Oxide.LogWarning($"Status code from download location was not okay (code {statusCode})");
                var remoteHash = response.Headers[HttpResponseHeader.ETag].Trim('"');
                var localHash = File.Exists(filePath) ? GetHash(filePath, Algorithms.MD5) : "0";
                Interface.Oxide.LogInfo($"Latest compiler MD5: {remoteHash}");
                Interface.Oxide.LogInfo($"Local compiler MD5: {localHash}");
                if (remoteHash != localHash)
                {
                    Interface.Oxide.LogInfo("Compiler hashes did not match, downloading latest");
                    DownloadCompiler(response, remoteHash);
                }
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogError($"Couldn't check for update to {FileName}");
                Interface.Oxide.LogError(ex.Message);
            }
        }

        private static void SetCompilerVersion()
        {
            var version = FileVersionInfo.GetVersionInfo(BinaryPath);
            CompilerVersion = $"{version.FileMajorPart}.{version.FileMinorPart}.{version.FileBuildPart}.{version.FilePrivatePart}";
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
                OnCompilerFailed($"compiler v{CompilerVersion} couldn't be started");
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
                    OnCompilerFailed($"compiler v{CompilerVersion} disconnected");
                    DependencyTrace();
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
                        Interface.Oxide.LogWarning("Compiler compiled an unknown assembly"); // TODO: Any way to clarify this?
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
                                    Interface.Oxide.LogError($"Unable to resolve script error to plugin: {line}");
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

            SetCompilerVersion();
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
                Interface.Oxide.LogException($"Exception while starting compiler v{CompilerVersion}: ", ex);
                if (BinaryPath.Contains("'")) Interface.Oxide.LogWarning("Server directory path contains an apostrophe, compiler will not work until path is renamed");
                else if (Environment.OSVersion.Platform == PlatformID.Unix) Interface.Oxide.LogWarning("Compiler may not be set as executable; chmod +x or 0744/0755 required");
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
                OnCompilerFailed($"compiler v{CompilerVersion} was closed unexpectedly");
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                    Interface.Oxide.LogWarning("User running server may not have the proper permissions or install is missing files");
                else
                    Interface.Oxide.LogWarning("Compiler may have been closed by interference from security software");
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

        private static class Algorithms
        {
            public static readonly HashAlgorithm MD5 = new MD5CryptoServiceProvider();
            public static readonly HashAlgorithm SHA1 = new SHA1Managed();
            public static readonly HashAlgorithm SHA256 = new SHA256Managed();
            public static readonly HashAlgorithm SHA384 = new SHA384Managed();
            public static readonly HashAlgorithm SHA512 = new SHA512Managed();
            public static readonly HashAlgorithm RIPEMD160 = new RIPEMD160Managed();
        }

        private static string GetHash(string filePath, HashAlgorithm algorithm)
        {
            using (var stream = new BufferedStream(File.OpenRead(filePath), 100000))
            {
                var hash = algorithm.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
        }
    }
}
