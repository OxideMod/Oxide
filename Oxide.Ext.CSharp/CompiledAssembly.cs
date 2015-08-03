using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

using Oxide.Core;

using MethodAttributes = Mono.Cecil.MethodAttributes;
using MethodBody = Mono.Cecil.Cil.MethodBody;

namespace Oxide.Plugins
{
    public class CompiledAssembly
    {
        public CompilablePlugin[] CompilablePlugins;
        public string[] PluginNames;
        public string Name;
        public byte[] RawAssembly;
        public Assembly LoadedAssembly;
        public bool IsLoading;
        public bool IsBatch => CompilablePlugins.Length > 1;

        private List<Action<bool>> loadCallbacks = new List<Action<bool>>();
        private bool isPatching;
        private bool isLoaded;

        private string[] blacklistedNamespaces => new[] {
            "System.IO", "System.Net", "System.Xml", "System.Reflection.Assembly", "System.Reflection.Emit", "System.Threading",
            "System.Runtime.InteropServices", "System.Diagnostics", "System.Security", "System.Timers", "Mono.CSharp", "Mono.Cecil",
            "ServerFileSystem"
        };

        private string[] whitelistedNamespaces => new[] {
            "System.IO.MemoryStream", "System.IO.BinaryReader", "System.IO.BinaryWriter", "System.Net.Sockets.SocketFlags",
            "System.Security.Cryptography"
        };

        public CompiledAssembly(string name, CompilablePlugin[] plugins, byte[] raw_assembly)
        {
            Name = name;
            CompilablePlugins = plugins;
            RawAssembly = raw_assembly;
            PluginNames = CompilablePlugins.Select(pl => pl.Name).ToArray();
        }

        public void LoadAssembly(Action<bool> callback)
        {
            if (isLoaded)
            {
                callback(true);
                return;
            }

            IsLoading = true;
            loadCallbacks.Add(callback);
            if (isPatching) return;

            //Interface.Oxide.LogDebug("Loading plugins: {0}", PluginNames.ToSentence());

            //var started_at = Interface.Oxide.Now;
            PatchAssembly(raw_assembly =>
            {
                //Interface.Oxide.LogInfo("Patching {0} took {1}ms", Name, Math.Round((Interface.Oxide.Now - started_at) * 1000f));
                if (raw_assembly == null)
                {
                    foreach (var cb in loadCallbacks) cb(true);
                    loadCallbacks.Clear();
                    IsLoading = false;
                    return;
                }

                LoadedAssembly = Assembly.Load(raw_assembly);
                isLoaded = true;

                foreach (var cb in loadCallbacks) cb(true);
                loadCallbacks.Clear();
                
                IsLoading = false;
            });
        }

        private void PatchAssembly(Action<byte[]> callback)
        {
            if (isPatching)
            {
                Interface.Oxide.LogWarning("Already patching plugin assembly: {0} (ignoring)", PluginNames.ToSentence());
                RemoteLogger.Warning("Already patching plugin assembly: " + PluginNames.ToSentence());
                return;
            }

            var started_at = Interface.Oxide.Now;

            //Interface.Oxide.LogInfo("Patching plugin assembly: {0}", Name);
            isPatching = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    AssemblyDefinition definition;
                    using (var stream = new MemoryStream(RawAssembly))
                        definition = AssemblyDefinition.ReadAssembly(stream);

                    var exception_constructor = typeof(UnauthorizedAccessException).GetConstructor(new Type[] { typeof(string) });
                    var security_exception = definition.MainModule.Import(exception_constructor);

                    Action<TypeDefinition> patch_module_type = null;
                    patch_module_type = type =>
                    {
                        foreach (var method in type.Methods)
                        {
                            Collection<Instruction> instructions = null;
                            var changed_method = false;

                            if (method.Body == null)
                            {
                                if (method.HasPInvokeInfo)
                                {
                                    method.Attributes &= ~MethodAttributes.PInvokeImpl;
                                    var body = new MethodBody(method);
                                    body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "PInvoke access is restricted, you are not allowed to use PInvoke"));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Newobj, security_exception));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                                    method.Body = body;
                                }
                            }
                            else
                            {
                                var replaced_method = false;
                                foreach (var variable in method.Body.Variables)
                                {
                                    if (IsNamespaceBlacklisted(variable.VariableType.FullName))
                                    {
                                        var body = new MethodBody(method);
                                        body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "System access is restricted, you are not allowed to use " + variable.VariableType.FullName));
                                        body.Instructions.Add(Instruction.Create(OpCodes.Newobj, security_exception));
                                        body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                                        method.Body = body;
                                        replaced_method = true;
                                        break;
                                    }
                                    if (replaced_method) break;
                                }
                                if (replaced_method) continue;

                                instructions = method.Body.Instructions;

                                var i = 0;
                                while (i < instructions.Count)
                                {
                                    var instruction = instructions[i];
                                    if (instruction.OpCode == OpCodes.Ldtoken)
                                    {
                                        var operand = instruction.Operand as IMetadataTokenProvider;
                                        var token = operand.ToString();
                                        if (IsNamespaceBlacklisted(token))
                                        {
                                            instructions[i++] = Instruction.Create(OpCodes.Ldstr, "System access is restricted, you are not allowed to use " + token);
                                            instructions.Insert(i++, Instruction.Create(OpCodes.Newobj, security_exception));
                                            instructions.Insert(i, Instruction.Create(OpCodes.Throw));
                                            changed_method = true;
                                        }
                                    }
                                    else if (instruction.OpCode == OpCodes.Call)
                                    {
                                        var method_call = instruction.Operand as MethodReference;
                                        var full_namespace = method_call.DeclaringType.FullName;
                                        
                                        if ((full_namespace == "System.Type" && method_call.Name == "GetType") || IsNamespaceBlacklisted(full_namespace))
                                        {
                                            for (var n = 0; n < method.Parameters.Count; n++)
                                                instructions.Insert(i++, Instruction.Create(OpCodes.Pop));

                                            instructions[i++] = Instruction.Create(OpCodes.Ldstr, "System access is restricted, you are not allowed to use " + full_namespace);
                                            instructions.Insert(i++, Instruction.Create(OpCodes.Newobj, security_exception));
                                            instructions.Insert(i, Instruction.Create(OpCodes.Throw));

                                            changed_method = true;
                                        }
                                    }
                                    i++;
                                }
                            }

                            if (changed_method)
                            {
                                //Interface.Oxide.LogDebug("Updating {0} instruction offsets: {1}", instructions.Count, method.FullName);
                                int curoffset = 0;
                                for (var i = 0; i < instructions.Count; i++)
                                {
                                    var instruction = instructions[i];
                                    instruction.Previous = (i == 0) ? null : instructions[i - 1];
                                    instruction.Next = (i == instructions.Count - 1) ? null : instructions[i + 1];
                                    instruction.Offset = curoffset;
                                    curoffset += instruction.GetSize();
                                    //Interface.Oxide.LogDebug("    {0}", instruction.ToString());
                                }
                            }
                        }
                        foreach (var nested_type in type.NestedTypes)
                            patch_module_type(nested_type);
                    };

                    foreach (var type in definition.MainModule.Types)
                    {
                        patch_module_type(type);

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

                    byte[] patched_assembly;
                    using (var stream = new MemoryStream())
                    {
                        definition.Write(stream);
                        patched_assembly = stream.ToArray();
                    }

                    Interface.Oxide.NextTick(() =>
                    {
                        isPatching = false;
                        //Interface.Oxide.LogDebug("Patching {0} assembly took {1:0.00} ms", ScriptName, Interface.Oxide.Now - started_at);
                        callback(patched_assembly);
                    });
                }
                catch (Exception ex)
                {
                    Interface.Oxide.NextTick(() =>
                    {
                        isPatching = false;
                        Interface.Oxide.LogException("Exception while patching: " + PluginNames.ToSentence(), ex);
                        RemoteLogger.Exception("Exception while patching: " + PluginNames.ToSentence(), ex);
                        callback(null);
                    });
                }
            });
        }

        private bool IsNamespaceBlacklisted(string full_namespace)
        {
            foreach (var namespace_name in blacklistedNamespaces)
            {
                if (!full_namespace.StartsWith(namespace_name)) continue;
                if (whitelistedNamespaces.Any(name => full_namespace.StartsWith(name))) continue;
                return true;
            }
            return false;
        }
    }
}
