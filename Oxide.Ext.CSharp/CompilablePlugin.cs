using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading;

using Mono.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

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
        public byte[] CompiledRawAssembly;
        public byte[] LastGoodRawAssembly;
        public DateTime LastModifiedAt;
        public DateTime LastCompiledAt;
        public int CompilationCount;
        public int LastGoodVersion;

        private PluginCompiler compiler;
        private bool isPatching = false;

        private string[] blacklistedNamespaces = {
            "System.IO", "System.Net", "System.Xml", "System.Reflection.Assembly", "System.Reflection.Emit", "System.Threading",
            "System.Runtime.InteropServices", "System.Diagnostics", "System.Security", "Mono.CSharp", "Mono.Cecil"
        };

        private string[] whitelistedNamespaces = {
            "System.IO.MemoryStream", "System.IO.BinaryReader", "System.IO.BinaryWriter", "System.Net.Sockets.SocketFlags"
        };

        public CompilablePlugin(CSharpExtension extension, string directory, string name)
        {
            Extension = extension;
            Directory = directory;
            ScriptName = name;
            Name = Regex.Replace(Regex.Replace(ScriptName, @"(?:^|_)([a-z])", m => m.Groups[1].Value.ToUpper()), "_", "");
            ScriptPath = string.Format("{0}\\{1}.cs", Directory, ScriptName);
            CheckLastModificationTime();
        }

        public void Compile(Action<bool> callback)
        {
            CheckLastModificationTime();
            if (LastCompiledAt == LastModifiedAt)
            {
                //Interface.Oxide.LogInfo("Plugin is already compiled: {0}", Name);
                callback(true);
                return;
            }
            if (compiler != null)
            {
                //Interface.Oxide.LogInfo("Plugin compilation is already in progress: {0}", ScriptName);
                return;
            }
            compiler = new PluginCompiler(this);
            compiler.Compile(raw_assembly =>
            {
                if (raw_assembly == null)
                {
                    LastCompiledAt = default(DateTime);
                    Interface.Oxide.LogError("{0} plugin failed to compile! Exit code: {1}", ScriptName, compiler.ExitCode);
                    Interface.Oxide.LogWarning(compiler.StdOutput.ToString());
                    if (compiler.ErrOutput.Length > 0) Interface.Oxide.LogError(compiler.ErrOutput.ToString());
                }
                else
                {
                    Interface.Oxide.LogInfo("{0} plugin was compiled successfully in {1}ms", ScriptName, Math.Round(compiler.Duration * 1000f));
                    CompiledRawAssembly = raw_assembly;
                }
                compiler = null;
                if (raw_assembly == null)
                {
                    callback(false);
                }
                else
                {
                    CheckLastModificationTime();
                    if (LastCompiledAt == LastModifiedAt)
                    {
                        callback(true);
                    }
                    else
                    {
                        Interface.Oxide.LogInfo("{0} plugin was changed during compilation and needs to be recompiled", ScriptName);
                        Compile(callback);
                    }
                }
            });
        }

        public void LoadAssembly(bool should_rollback, Action<CSharpPlugin> callback)
        {
            //Interface.Oxide.LogInfo("Loading plugin: {0}_{1}", Name, version);

            var started_at = UnityEngine.Time.realtimeSinceStartup;

            PatchAssembly(should_rollback, raw_assembly =>
            {
                //Interface.Oxide.LogInfo("Patching {0} took {1}ms", Name, Math.Round((UnityEngine.Time.realtimeSinceStartup - started_at) * 1000f));

                var assembly = Assembly.Load(raw_assembly);

                var type = assembly.GetType("Oxide.Plugins." + Name);
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
                    OnPluginFailed();
                    if (callback != null) callback(null);
                    return;
                }

                plugin.SetPluginInfo(ScriptName, ScriptPath);
                plugin.Watcher = Extension.Watcher;

                if (Interface.Oxide.PluginLoaded(plugin))
                {
                    LastGoodVersion = CompilationCount;
                    LastGoodRawAssembly = raw_assembly;
                    if (callback != null) callback(plugin);
                }
                else
                {
                    // Plugin failed to be initialized
                    OnPluginFailed();
                    if (callback != null) callback(null);
                }
            });
        }

        public void LoadAssembly(Action<CSharpPlugin> callback)
        {
            LoadAssembly(false, callback);
        }

        public void OnCompilerStarted()
        {
            //Interface.Oxide.LogInfo("Compiling plugin: {0}", Name);
            LastCompiledAt = LastModifiedAt;
            CompilationCount++;
        }

        public void OnPluginFailed()
        {
            if (LastGoodVersion > 0)
            {
                Interface.Oxide.LogInfo("Rolling back plugin to version {0}: {1}", LastGoodVersion, ScriptName);
                LoadAssembly(true, null);
            }
            else
            {
                Interface.Oxide.LogInfo("No previous version to rollback plugin: {0}", ScriptName);
            }
        }

        private void PatchAssembly(bool last_good_version, Action<byte[]> callback)
        {
            if (isPatching)
            {
                Interface.Oxide.LogWarning("Already patching plugin assembly: {0} (ignoring)", ScriptName);
                return;
            }
            
            var raw_assembly = last_good_version ? LastGoodRawAssembly : CompiledRawAssembly;
            var started_at = UnityEngine.Time.realtimeSinceStartup;

            //Interface.Oxide.LogInfo("Patching plugin assembly: {0}", Name);
            isPatching = true;
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    AssemblyDefinition definition;
                    using (var stream = new MemoryStream(raw_assembly))
                        definition = AssemblyDefinition.ReadAssembly(stream);

                    var exception_constructor = typeof(UnauthorizedAccessException).GetConstructor(new Type[] { typeof(string) });
                    var security_exception = definition.MainModule.Import(exception_constructor);

                    Action<TypeDefinition> patch_module_type = null;
                    patch_module_type = (type) =>
                    {
                        foreach (var method in type.Methods)
                        {
                            Collection<Instruction> instructions = null;
                            var changed_method = false;

                            if (method.Body == null)
                            {
                                if (method.HasPInvokeInfo)
                                {
                                    method.Attributes &= ~Mono.Cecil.MethodAttributes.PInvokeImpl;
                                    var body = new Mono.Cecil.Cil.MethodBody(method);
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
                                    foreach (var namespace_name in blacklistedNamespaces)
                                        if (variable.VariableType.FullName.StartsWith(namespace_name))
                                        {
                                            if (whitelistedNamespaces.Any(name => variable.VariableType.FullName.StartsWith(name))) continue;
                                            var body = new Mono.Cecil.Cil.MethodBody(method);
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
                                        foreach (var namespace_name in blacklistedNamespaces)
                                            if (token.StartsWith(namespace_name))
                                            {
                                                if (whitelistedNamespaces.Any(name => token.StartsWith(name))) continue;
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

                                        foreach (var namespace_name in blacklistedNamespaces)
                                            if (full_namespace.StartsWith(namespace_name))
                                            {
                                                if (whitelistedNamespaces.Any(name => full_namespace.StartsWith(name))) continue;

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
                        patch_module_type(type);

                    byte[] patched_assembly;
                    using (var stream = new MemoryStream())
                    {
                        definition.Write(stream);
                        patched_assembly = stream.ToArray();
                    }

                    Interface.Oxide.NextTick(() =>
                    {
                        isPatching = false;
                        //Interface.Oxide.LogDebug("Patching {0} assembly took {1:0.00} ms", ScriptName, UnityEngine.Time.realtimeSinceStartup - started_at);
                        callback(patched_assembly);
                    });
                }
                catch (Exception ex)
                {
                    isPatching = false;
                    Interface.Oxide.NextTick(() => Interface.Oxide.LogException("Exception while patching " + ScriptName, ex));
                }
            });
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
