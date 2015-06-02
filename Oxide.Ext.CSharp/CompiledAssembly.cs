using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Mono.Cecil;

using Oxide.Core;

namespace Oxide.Plugins
{
    public class CompiledAssembly
    {
        public CompilablePlugin[] CompilablePlugins;
        public string[] PluginNames;
        public byte[] RawAssembly;
        public Assembly LoadedAssembly;
        public bool IsBatch => CompilablePlugins.Length > 1;

        private List<Action<bool>> loadCallbacks = new List<Action<bool>>();
        private bool _isPatching;
        private bool _isLoaded;
		
        public CompiledAssembly(CompilablePlugin[] plugins, byte[] rawAssembly)
        {
            CompilablePlugins = plugins;
            RawAssembly = rawAssembly;
            PluginNames = CompilablePlugins.Select(pl => pl.Name).ToArray();
        }

        public void LoadAssembly(Action<bool> callback)
        {
            if (_isLoaded)
            {
                callback(true);
                return;
            }

            loadCallbacks.Add(callback);
            if (_isPatching) return;

            //Interface.Oxide.LogDebug("Loading plugins: {0}", PluginNames.ToSentence());

            //var started_at = Interface.Oxide.Now;
            PatchAssembly(rawAssembly =>
            {
                //Interface.Oxide.LogInfo("Patching {0} took {1}ms", Name, Math.Round((Interface.Oxide.Now - started_at) * 1000f));
                if (rawAssembly == null)
                {
                    callback(false);
                    return;
                }

                LoadedAssembly = Assembly.Load(rawAssembly);
                _isLoaded = true;

                foreach (var cb in loadCallbacks) cb(true);
                loadCallbacks.Clear();
            });
        }

        private void PatchAssembly(Action<byte[]> callback)
        {
            if (_isPatching)
            {
                Interface.Oxide.LogWarning("Already patching plugin assembly: {0} (ignoring)", PluginNames.ToSentence());
                RemoteLogger.Warning("Already patching plugin assembly: " + PluginNames.ToSentence());
                return;
            }
			
            //Interface.Oxide.LogInfo("Patching plugin assembly: {0}", Name);
            _isPatching = true;
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    AssemblyDefinition definition;
                    using (var stream = new MemoryStream(RawAssembly))
                        definition = AssemblyDefinition.ReadAssembly(stream);

                    foreach (var type in definition.MainModule.Types)
                    {
						// TODO: Remove this so we can deal with the namespace change for the main plugin class.
						// Honestly, this can be removed entirely. Since we basically use Activator.CreateInstance, the ctor is called regardless of whether it's public or not.
                        if (type.Namespace == "Oxide.Plugins")
                        {
                            if (PluginNames.Contains(type.Name))
                            {
                                var constructor = type.Methods.FirstOrDefault(m => !m.IsStatic && m.IsConstructor && !m.HasParameters && !m.IsPublic);
                                if (constructor != null)
                                {
                                    var plugin = CompilablePlugins.SingleOrDefault(p => p.Name == type.Name);
                                    plugin.CompilerErrors = "Primary constructor in main class must be public";
                                }
                            }
                            else
                            {
                                Interface.Oxide.LogWarning("A plugin has polluted the global namespace by defining " + type.Name + ": " + PluginNames.ToSentence());
                                RemoteLogger.Info("A plugin has polluted the global namespace by defining " + type.Name + ": " + PluginNames.ToSentence());
                            }
                        }
                    }
					
                    Interface.Oxide.NextTick(() =>
                    {
                        _isPatching = false;
                        //Interface.Oxide.LogDebug("Patching {0} assembly took {1:0.00} ms", ScriptName, Interface.Oxide.Now - started_at);
                        callback(RawAssembly);
                    });
                }
                catch (Exception ex)
                {
                    Interface.Oxide.NextTick(() =>
                    {
                        _isPatching = false;
                        Interface.Oxide.LogException("Exception while patching: " + PluginNames.ToSentence(), ex);
                        RemoteLogger.Exception("Exception while patching: " + PluginNames.ToSentence(), ex);
                        callback(null);
                    });
                }
            });
        }
    }
}
