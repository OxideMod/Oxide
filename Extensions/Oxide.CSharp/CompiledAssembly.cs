extern alias Oxide;

using Oxide.Core;
using Oxide.Core.CSharp;
using Oxide::Mono.Cecil;
using Oxide::Mono.Cecil.Cil;
using Oxide::Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using MethodAttributes = Oxide::Mono.Cecil.MethodAttributes;
using MethodBody = Oxide::Mono.Cecil.Cil.MethodBody;

namespace Oxide.Plugins
{
    public class CompiledAssembly
    {
        public CompilablePlugin[] CompilablePlugins;
        public string[] PluginNames;
        public string Name;
        public DateTime CompiledAt;
        public byte[] RawAssembly;
        public byte[] PatchedAssembly;
        public float Duration;
        public Assembly LoadedAssembly;
        public bool IsLoading;
        public bool IsBatch => CompilablePlugins.Length > 1;

        private List<Action<bool>> loadCallbacks = new List<Action<bool>>();
        private bool isPatching;
        private bool isLoaded;

        private static IEnumerable<string> BlacklistedNamespaces => new[] {
            "Oxide.Core.ServerConsole", "System.IO", "System.Net", "System.Xml", "System.Reflection.Assembly", "System.Reflection.Emit", "System.Threading",
            "System.Runtime.InteropServices", "System.Diagnostics", "System.Security", "System.Timers", "Mono.CSharp", "Mono.Cecil",
            "ServerFileSystem"
        };

        private static IEnumerable<string> WhitelistedNamespaces => new[] {
            "System.Diagnostics.Stopwatch", "System.IO.MemoryStream", "System.IO.Stream", "System.IO.BinaryReader", "System.IO.BinaryWriter",
            "System.Net.Dns.GetHostEntry", "System.Net.Sockets.SocketFlags", "System.Net.IPEndPoint", "System.Security.Cryptography", "System.Threading.Interlocked"
        };

        public CompiledAssembly(string name, CompilablePlugin[] plugins, byte[] rawAssembly, float duration)
        {
            Name = name;
            CompilablePlugins = plugins;
            RawAssembly = rawAssembly;
            Duration = duration;
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

            PatchAssembly(rawAssembly =>
            {
                if (rawAssembly == null)
                {
                    foreach (var cb in loadCallbacks) cb(true);
                    loadCallbacks.Clear();
                    IsLoading = false;
                    return;
                }

                LoadedAssembly = Assembly.Load(rawAssembly);
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
                //RemoteLogger.Warning($"Already patching plugin assembly: {PluginNames.ToSentence()}");
                return;
            }

            var startedAt = Interface.Oxide.Now;

            isPatching = true;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    AssemblyDefinition definition;
                    using (var stream = new MemoryStream(RawAssembly))
                        definition = AssemblyDefinition.ReadAssembly(stream);

                    var exceptionConstructor = typeof(UnauthorizedAccessException).GetConstructor(new[] { typeof(string) });
                    var securityException = definition.MainModule.Import(exceptionConstructor);

                    Action<TypeDefinition> patchModuleType = null;
                    patchModuleType = type =>
                    {
                        foreach (var method in type.Methods)
                        {
                            var changedMethod = false;

                            if (method.Body == null)
                            {
                                if (method.HasPInvokeInfo)
                                {
                                    method.Attributes &= ~MethodAttributes.PInvokeImpl;
                                    var body = new MethodBody(method);
                                    body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "PInvoke access is restricted, you are not allowed to use PInvoke"));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Newobj, securityException));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                                    method.Body = body;
                                }
                            }
                            else
                            {
                                var replacedMethod = false;
                                foreach (var variable in method.Body.Variables)
                                {
                                    if (!IsNamespaceBlacklisted(variable.VariableType.FullName)) continue;

                                    var body = new MethodBody(method);
                                    body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, $"System access is restricted, you are not allowed to use {variable.VariableType.FullName}"));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Newobj, securityException));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                                    method.Body = body;
                                    replacedMethod = true;
                                    break;
                                }
                                if (replacedMethod) continue;

                                var instructions = method.Body.Instructions;
                                var ilProcessor = method.Body.GetILProcessor();
                                var first = instructions.First();

                                var i = 0;
                                while (i < instructions.Count)
                                {
                                    if (changedMethod) break;
                                    var instruction = instructions[i];
                                    if (instruction.OpCode == OpCodes.Ldtoken)
                                    {
                                        var operand = instruction.Operand as IMetadataTokenProvider;
                                        var token = operand?.ToString();
                                        if (IsNamespaceBlacklisted(token))
                                        {
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, $"System access is restricted, you are not allowed to use {token}"));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Newobj, securityException));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Throw));
                                            changedMethod = true;
                                        }
                                    }
                                    else if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli || instruction.OpCode == OpCodes.Callvirt || instruction.OpCode == OpCodes.Ldftn)
                                    {
                                        var methodCall = instruction.Operand as MethodReference;
                                        var fullNamespace = methodCall?.DeclaringType.FullName;

                                        if ((fullNamespace == "System.Type" && methodCall.Name == "GetType") || IsNamespaceBlacklisted(fullNamespace))
                                        {
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, $"System access is restricted, you are not allowed to use {fullNamespace}"));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Newobj, securityException));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Throw));
                                            changedMethod = true;
                                        }
                                    }
                                    else if (instruction.OpCode == OpCodes.Ldfld)
                                    {
                                        var fieldType = instruction.Operand as FieldReference;
                                        var fullNamespace = fieldType?.FieldType.FullName;
                                        if (IsNamespaceBlacklisted(fullNamespace))
                                        {
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Ldstr, $"System access is restricted, you are not allowed to use {fullNamespace}"));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Newobj, securityException));
                                            ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Throw));
                                            changedMethod = true;
                                        }
                                    }
                                    i++;
                                }
                            }

                            if (changedMethod)
                            {
                                method.Body?.OptimizeMacros();
                                /*//Interface.Oxide.LogDebug("Updating {0} instruction offsets: {1}", instructions.Count, method.FullName);
                                int curoffset = 0;
                                for (var i = 0; i < instructions.Count; i++)
                                {
                                    var instruction = instructions[i];
                                    instruction.Previous = (i == 0) ? null : instructions[i - 1];
                                    instruction.Next = (i == instructions.Count - 1) ? null : instructions[i + 1];
                                    instruction.Offset = curoffset;
                                    curoffset += instruction.GetSize();
                                    //Interface.Oxide.LogDebug("    {0}", instruction.ToString());
                                }*/
                            }
                        }
                        foreach (var nestedType in type.NestedTypes)
                            patchModuleType(nestedType);
                    };

                    foreach (var type in definition.MainModule.Types)
                    {
                        patchModuleType(type);

                        if (IsCompilerGenerated(type)) continue;

                        if (type.Namespace == "Oxide.Plugins")
                        {
                            if (PluginNames.Contains(type.Name))
                            {
                                var constructor =
                                    type.Methods.FirstOrDefault(
                                        m => !m.IsStatic && m.IsConstructor && !m.HasParameters && !m.IsPublic);
                                if (constructor != null)
                                {
                                    var plugin = CompilablePlugins.SingleOrDefault(p => p.Name == type.Name);
                                    if (plugin != null)
                                        plugin.CompilerErrors = "Primary constructor in main class must be public";
                                }
                                else
                                {
                                    new DirectCallMethod(definition.MainModule, type);
                                }
                            }
                            else
                            {
                                Interface.Oxide.LogWarning(PluginNames.Length == 1
                                    ? $"{PluginNames[0]} has polluted the global namespace by defining {type.Name}"
                                    : $"A plugin has polluted the global namespace by defining {type.Name}");
                                //RemoteLogger.Info($"A plugin has polluted the global namespace by defining {type.Name}: {PluginNames.ToSentence()}");
                            }
                        }
                        else if (type.FullName != "<Module>")
                        {
                            if (!PluginNames.Any(plugin => type.FullName.StartsWith($"Oxide.Plugins.{plugin}")))
                                Interface.Oxide.LogWarning(PluginNames.Length == 1
                                    ? $"{PluginNames[0]} has polluted the global namespace by defining {type.FullName}"
                                    : $"A plugin has polluted the global namespace by defining {type.FullName}");
                        }
                    }

                    // TODO: Why is there no error on boot using this?
                    foreach (var type in definition.MainModule.Types)
                    {
                        if (type.Namespace != "Oxide.Plugins" || !PluginNames.Contains(type.Name)) continue;
                        foreach (var m in type.Methods.Where(m => !m.IsStatic && !m.HasGenericParameters && !m.ReturnType.IsGenericParameter && !m.IsSetter && !m.IsGetter))
                        {
                            foreach (var parameter in m.Parameters)
                            {
                                foreach (var attribute in parameter.CustomAttributes)
                                {
                                    //Interface.Oxide.LogInfo($"{m.FullName} - {parameter.Name} - {attribute.Constructor.FullName}");
                                }
                            }
                        }
                    }

                    using (var stream = new MemoryStream())
                    {
                        definition.Write(stream);
                        PatchedAssembly = stream.ToArray();
                    }

                    Interface.Oxide.NextTick(() =>
                    {
                        isPatching = false;
                        //Interface.Oxide.LogDebug("Patching {0} assembly took {1:0.00} ms", ScriptName, Interface.Oxide.Now - startedAt);
                        callback(PatchedAssembly);
                    });
                }
                catch (Exception ex)
                {
                    Interface.Oxide.NextTick(() =>
                    {
                        isPatching = false;
                        Interface.Oxide.LogException($"Exception while patching: {PluginNames.ToSentence()}", ex);
                        //RemoteLogger.Exception($"Exception while patching: {PluginNames.ToSentence()}", ex);
                        callback(null);
                    });
                }
            });
        }

        public bool IsOutdated() => CompilablePlugins.Any(pl => pl.GetLastModificationTime() != CompiledAt);

        private bool IsCompilerGenerated(TypeDefinition type) => type.CustomAttributes.Any(attr => attr.Constructor.DeclaringType.ToString().Contains("CompilerGeneratedAttribute"));

        private static bool IsNamespaceBlacklisted(string fullNamespace)
        {
            foreach (var namespaceName in BlacklistedNamespaces)
            {
                if (!fullNamespace.StartsWith(namespaceName)) continue;
                if (WhitelistedNamespaces.Any(fullNamespace.StartsWith)) continue;
                return true;
            }
            return false;
        }
    }
}
