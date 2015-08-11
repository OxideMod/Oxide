using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    public class CompilablePlugin
    {
        public CSharpExtension Extension;
        public CSharpPluginLoader Loader;
        public string Name;
        public string Directory;
        public string ScriptName;
        public string ScriptPath;
        public string[] ScriptLines;
        public Encoding ScriptEncoding;
        public HashSet<string> Requires = new HashSet<string>();
        public HashSet<string> References = new HashSet<string>();
        public HashSet<string> IncludePaths = new HashSet<string>();
        public string CompilerErrors;
        public CompiledAssembly CompiledAssembly;
        public CompiledAssembly LastGoodAssembly;
        public DateTime LastModifiedAt;
        public DateTime LastCompiledAt;
        public bool IsCompilationNeeded;
        public bool IsReloading;

        private Action<CSharpPlugin> loadCallback;
        private Action<bool> compileCallback;
        private float compilationQueuedAt;

        public byte[] ScriptSource => ScriptEncoding.GetBytes(string.Join(Environment.NewLine, ScriptLines));

        public CompilablePlugin(CSharpExtension extension, CSharpPluginLoader loader, string directory, string name)
        {
            Extension = extension;
            Loader = loader;
            Directory = directory;
            ScriptName = name;
            Name = Regex.Replace(Regex.Replace(ScriptName, @"(?:^|_)([a-z])", m => m.Groups[1].Value.ToUpper()), "_", "");
            ScriptPath = Path.Combine(Directory, string.Format("{0}.cs", ScriptName));
            CheckLastModificationTime();
        }

        public void Compile(Action<bool> callback, bool queue_compilation = true)
        {
            if (compilationQueuedAt > 0f)
            {
                var ago = Interface.Oxide.Now - compilationQueuedAt;
                Interface.Oxide.LogDebug($"Plugin compilation is already queued: {ScriptName} ({ago:0.000} ago)");
                RemoteLogger.Debug($"Plugin compilation is already queued: {ScriptName} ({ago:0.000} ago)");
                return;
            }
            if (queue_compilation && CompiledAssembly != null && !HasBeenModified())
            {
                if (CompiledAssembly.IsLoading || !CompiledAssembly.IsBatch || CompiledAssembly.CompilablePlugins.All(pl => pl.IsReloading))
                {
                    //Interface.Oxide.LogDebug("Plugin is already compiled: {0}", Name);
                    callback(true);
                    return;
                }
            }
            compileCallback = callback;
            compilationQueuedAt = Interface.Oxide.Now;
            if (queue_compilation) Extension.CompilationRequested(this);
        }

        public void LoadPlugin(Action<CSharpPlugin> callback = null)
        {
            if (CompiledAssembly == null)
            {
                Interface.Oxide.LogError("Load called before a compiled assembly exists: " + Name);
                RemoteLogger.Error("Load called before a compiled assembly exists: " + Name);
                IsReloading = false;
                return;
            }

            loadCallback = callback;

            CompiledAssembly.LoadAssembly(loaded =>
            {
                IsReloading = false;
                if (!loaded)
                {
                    if (callback != null) callback(null);
                    return;
                }

                if (CompilerErrors != null)
                {
                    InitFailed("Unable to load " + ScriptName + ". " + CompilerErrors);
                    return;
                }

                var type = CompiledAssembly.LoadedAssembly.GetType("Oxide.Plugins." + Name);
                if (type == null)
                {
                    InitFailed("Unable to find main plugin class: " + Name);
                    return;
                }

                CSharpPlugin plugin = null;
                try
                {
                    plugin = Activator.CreateInstance(type) as CSharpPlugin;
                }
                catch (MissingMethodException)
                {
                    InitFailed("Main plugin class should not have a constructor defined: " + Name);
                    return;
                }
                catch (TargetInvocationException invocation_exception)
                {
                    var ex = invocation_exception.InnerException;
                    InitFailed("Unable to load " + ScriptName + ". " + ex.ToString());
                    return;
                }
                catch (Exception ex)
                {
                    InitFailed("Unable to load " + ScriptName + ". " + ex.ToString());
                    return;
                }

                if (plugin == null)
                {
                    RemoteLogger.Error("Plugin assembly failed to load: " + ScriptName);
                    InitFailed("Plugin assembly failed to load: " + ScriptName);
                    return;
                }

                plugin.SetPluginInfo(ScriptName, ScriptPath);
                plugin.Watcher = Extension.Watcher;
                plugin.Loader = Loader;

                if (!Interface.Oxide.PluginLoaded(plugin))
                {
                    InitFailed();
                    return;
                }

                if (!CompiledAssembly.IsBatch) LastGoodAssembly = CompiledAssembly;
                if (callback != null) callback(plugin);
            });
        }

        public void OnCompilationStarted(PluginCompiler compiler)
        {
            //Interface.Oxide.LogDebug("Compiling plugin: {0}", Name);
            LastCompiledAt = LastModifiedAt;
        }

        public void OnCompilationSucceeded(CompiledAssembly compiled_assembly)
        {
            IsCompilationNeeded = false;
            compilationQueuedAt = 0f;
            CompiledAssembly = compiled_assembly;
            compileCallback(true);
        }

        public void OnCompilationFailed()
        {
            IsCompilationNeeded = false;
            compilationQueuedAt = 0f;
            LastCompiledAt = default(DateTime);
            compileCallback(false);
        }

        private void OnPluginFailed()
        {
            if (LastGoodAssembly == null)
            {
                Interface.Oxide.LogInfo("No previous version to rollback plugin: {0}", ScriptName);
                return;
            }
            Interface.Oxide.LogInfo("Rolling back plugin to last good version: {0}", ScriptName);
            CompiledAssembly = LastGoodAssembly;
            CompilerErrors = null;
            LoadPlugin();
        }

        private void InitFailed(string message = null)
        {
            if (message != null) Interface.Oxide.LogError(message);
            if (loadCallback != null) loadCallback(null);
            OnPluginFailed();
        }

        public bool IsCompiledAssemblyOutdated()
        {
            var last_modified_at = GetLastModificationTime();
            return last_modified_at != default(DateTime) && last_modified_at != LastCompiledAt;
        }

        public bool HasBeenModified()
        {
            var last_modified_at = LastModifiedAt;
            CheckLastModificationTime();
            return LastModifiedAt != last_modified_at;
        }

        private void CheckLastModificationTime()
        {
            if (!File.Exists(ScriptPath)) return;
            var modified_time = GetLastModificationTime();
            if (modified_time != default(DateTime)) LastModifiedAt = modified_time;
        }

        private DateTime GetLastModificationTime()
        {
            try
            {
                return File.GetLastWriteTime(ScriptPath);
            }
            catch (IOException ex)
            {
                Interface.Oxide.LogError("IOException while checking plugin: {0} ({1})", ScriptName, ex.Message);
                return default(DateTime);
            }
        }
    }
}
