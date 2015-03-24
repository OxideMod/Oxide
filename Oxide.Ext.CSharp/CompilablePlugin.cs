using System;
using System.Text.RegularExpressions;
using System.IO;

using Oxide.Core;

namespace Oxide.Plugins
{
    public class CompilablePlugin
    {
        public CSharpExtension Extension;
        public string Name;
        public string Directory;
        public string ScriptName;
        public string ScriptPath;
        public string[] ScriptLines;
        public CompiledAssembly CompiledAssembly;
        public CompiledAssembly LastGoodAssembly;
        public DateTime LastModifiedAt;
        public DateTime LastCompiledAt;

        private Action<bool> callback;
        private bool isCompilationQueued;

        public CompilablePlugin(CSharpExtension extension, string directory, string name)
        {
            Extension = extension;
            Directory = directory;
            ScriptName = name;
            Name = Regex.Replace(Regex.Replace(ScriptName, @"(?:^|_)([a-z])", m => m.Groups[1].Value.ToUpper()), "_", "");
            ScriptPath = string.Format("{0}\\{1}.cs", Directory, ScriptName);
            CheckLastModificationTime();
        }

        public bool HasBeenModified()
        {
            var last_modified_at = LastModifiedAt;
            CheckLastModificationTime();
            return LastModifiedAt != last_modified_at;
        }

        public void Compile(Action<bool> callback)
        {
            if (isCompilationQueued)
            {
                Interface.Oxide.LogDebug("Plugin compilation is already queued: {0}", ScriptName);
                return;
            }
            CheckLastModificationTime();
            if (CompiledAssembly != null && !CompiledAssembly.IsBatch && LastCompiledAt == LastModifiedAt)
            {
                Interface.Oxide.LogDebug("Plugin is already compiled: {0}", Name);
                callback(true);
                return;
            }
            this.callback = callback;
            isCompilationQueued = true;
            Extension.CompilationRequested(this);
        }

        public void LoadPlugin(Action<CSharpPlugin> callback = null)
        {
            if (CompiledAssembly == null)
            { 
                Interface.Oxide.LogError("Load called before a compiled assembly exists: " + Name);
                return;
            }

            CompiledAssembly.LoadAssembly(loaded =>
            {
                if (loaded)
                {
                    var type = CompiledAssembly.LoadedAssembly.GetType("Oxide.Plugins." + Name);
                    if (type == null)
                    {
                        Interface.Oxide.LogError("Unable to find main plugin class: {0}", Name);
                        OnPluginFailed();
                        if (callback != null) callback(null);
                        return;
                    }

                    var plugin = Activator.CreateInstance(type) as CSharpPlugin;
                    if (plugin == null)
                    {
                        Interface.Oxide.LogError("Plugin assembly failed to load: {0}", ScriptName);
                        RemoteLogger.Error("Plugin assembly failed to load: " + ScriptName);
                        OnPluginFailed();
                        if (callback != null) callback(null);
                        return;
                    }

                    plugin.SetPluginInfo(ScriptName, ScriptPath);
                    plugin.Watcher = Extension.Watcher;

                    if (Interface.Oxide.PluginLoaded(plugin))
                    {
                        if (!CompiledAssembly.IsBatch) LastGoodAssembly = CompiledAssembly;
                        if (callback != null) callback(plugin);
                        return;
                    }

                    OnPluginFailed();
                }
                if (callback != null) callback(null);
            });
        }

        public void OnCompilationStarted(PluginCompiler compiler)
        {
            //Interface.Oxide.LogDebug("Compiling plugin: {0}", Name);
            LastCompiledAt = LastModifiedAt;
        }

        public void OnCompilationSucceeded(CompiledAssembly compiled_assembly)
        {
            CheckLastModificationTime();
            if (LastCompiledAt != LastModifiedAt)
            {
                Interface.Oxide.LogInfo("{0} plugin was changed during compilation and needs to be recompiled", ScriptName);
                Extension.CompilationRequested(this);
                return;
            }
            isCompilationQueued = false;
            CompiledAssembly = compiled_assembly;
            callback(true);
        }

        public void OnCompilationFailed()
        {
            isCompilationQueued = false;
            LastCompiledAt = default(DateTime);
            callback(false);
        }

        private void OnPluginFailed()
        {
            if (LastGoodAssembly == null)
            {
                Interface.Oxide.LogInfo("No previous version to rollback plugin: {0}", ScriptName);
                return;
            }
            Interface.Oxide.LogInfo("Rolling back plugin to last good version: {1}", ScriptName);
            CompiledAssembly = LastGoodAssembly;
            LoadPlugin();
        }
        
        private void CheckLastModificationTime()
        {
            if (!File.Exists(ScriptPath)) return;
            try
            {
                LastModifiedAt = File.GetLastWriteTime(ScriptPath);
            }
            catch (IOException ex)
            {
                Interface.Oxide.LogError("IOException while checking plugin: {0} ({1})", ScriptName, ex.Message);
            }
        }
    }
}
