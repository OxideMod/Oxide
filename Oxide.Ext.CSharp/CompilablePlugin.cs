using System;
using System.Text.RegularExpressions;
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
        public DateTime LastModifiedAt;
        public DateTime LastCompiledAt;
        public int CompilationCount;
        public int LastGoodVersion;

        private PluginCompiler compiler;
        private bool isPatching = false;

        private string[] blacklistedNamespaces = {
            "System.IO", "System.Diagnostics", "System.Threading", "System.Reflection.Assembly", "System.Runtime.InteropServices", "System.Net",
            "Mono.Cecil"
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
                //Interface.GetMod().LogInfo("Plugin is already compiled: {0}", Name);
                callback(true);
                return;
            }
            if (compiler != null)
            {
                Interface.GetMod().LogInfo("Plugin compilation is already in progress: {0}", ScriptName);
                return;
            }
            compiler = new PluginCompiler(this);
            compiler.Compile(compiled =>
            {
                if (compiled)
                {
                    Interface.GetMod().LogInfo("{0} plugin was compiled successfully in {1}ms", ScriptName, Math.Round(compiler.Duration * 1000f));
                }
                else
                {
                    Interface.GetMod().LogInfo("{0} plugin failed to compile! Exit code: {1}", ScriptName, compiler.ExitCode);
                    Interface.GetMod().LogInfo(compiler.StdOutput.ToString());
                    if (compiler.ErrOutput.Length > 0) Interface.GetMod().LogInfo(compiler.ErrOutput.ToString());
                }
                callback(compiled);
                compiler = null;
            });
        }

        public void LoadAssembly(int version, Action<bool> callback)
        {
            //Interface.GetMod().LogInfo("Loading plugin: {0}_{1}", Name, version);

            var started_at = UnityEngine.Time.realtimeSinceStartup;
            var assembly_path = string.Format("{0}\\{1}_{2}.dll", Interface.GetMod().TempDirectory, Name, version);

            PatchAssembly(version, () =>
            {
                //Interface.GetMod().LogInfo("Patching {0} took {1}ms", Name, Math.Round((UnityEngine.Time.realtimeSinceStartup - started_at) * 1000f));

                var assembly = Assembly.LoadFrom(assembly_path);

                var type = assembly.GetType("Oxide.Plugins." + Name);
                if (type == null)
                {
                    Interface.GetMod().LogInfo("Unable to find main plugin class: {0}", Name);
                    return;
                }

                var plugin = Activator.CreateInstance(type) as CSharpPlugin;
                if (plugin == null)
                {
                    Interface.GetMod().LogInfo("Plugin assembly failed to load: {0} (version {1})", ScriptName, version);
                    return;
                }

                plugin.SetPluginInfo(ScriptName, ScriptPath);
                plugin.Watcher = Extension.Watcher;
                
                if (Interface.GetMod().PluginLoaded(plugin))
                {
                    LastGoodVersion = CompilationCount;
                    if (callback != null) callback(true);
                }
                else
                {
                    // Plugin failed to be initialized
                    if (LastGoodVersion > 0)
                    {
                        Interface.GetMod().LogInfo("Rolling back plugin to version {0}: {1}", LastGoodVersion, ScriptName);
                        LoadAssembly(LastGoodVersion, null);
                    }
                    else
                    {
                        Interface.GetMod().LogInfo("No previous version to rollback plugin: {0}", ScriptName);
                    }
                    if (callback != null) callback(false);
                }
            });
        }

        public void LoadAssembly(Action<bool> callback)
        {
            LoadAssembly(CompilationCount, callback);
        }

        public void OnCompilerStarted()
        {
            //Interface.GetMod().LogInfo("Compiling plugin: {0}", Name);
            LastCompiledAt = LastModifiedAt;
            CompilationCount++;
        }

        private void PatchAssembly(int version, Action callback)
        {
            if (isPatching)
            {
                Interface.GetMod().LogInfo("Already patching plugin assembly: {0} (ignoring)", ScriptName);
                return;
            }

            if (version == LastGoodVersion)
            {
                //Interface.GetMod().LogInfo("Plugin assembly has already been patched: {0}", Name);
                callback();
                return;
            }

            var path = string.Format("{0}\\{1}_{2}.dll", Interface.GetMod().TempDirectory, Name, version);

            //Interface.GetMod().LogInfo("Patching plugin assembly: {0}", Name);
            isPatching = true;
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    var definition = AssemblyDefinition.ReadAssembly(path);
                    var exception_constructor = typeof(UnauthorizedAccessException).GetConstructor(new Type[] { typeof(string) });
                    var security_exception = definition.MainModule.Import(exception_constructor);

                    foreach (var type in definition.MainModule.Types)
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
                                    body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Ldstr, "PInvoke access is restricted"));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Newobj, security_exception));
                                    body.Instructions.Add(Instruction.Create(OpCodes.Throw));
                                    method.Body = body;
                                }
                            }
                            else
                            {
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
                                                instructions[i++] = Instruction.Create(OpCodes.Ldstr, "System access is restricted");
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
                                                for (var n = 0; n < method.Parameters.Count; n++)
                                                    instructions.Insert(i++, Instruction.Create(OpCodes.Pop));

                                                instructions[i++] = Instruction.Create(OpCodes.Ldstr, "System access is restricted");
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
                                //Interface.GetMod().LogInfo("Updating {0} instruction offsets: {1}", instructions.Count, method.FullName);
                                int curoffset = 0;
                                for (var i = 0; i < instructions.Count; i++)
                                {
                                    var instruction = instructions[i];
                                    instruction.Previous = (i == 0) ? null : instructions[i - 1];
                                    instruction.Next = (i == instructions.Count - 1) ? null : instructions[i + 1];
                                    instruction.Offset = curoffset;
                                    curoffset += instruction.GetSize();
                                    //Interface.GetMod().LogInfo("    {0}", instruction.ToString());
                                }
                            }
                        }
                    }

                    definition.Write(path);

                    Interface.GetMod().NextTick(() =>
                    {
                        isPatching = false;
                        callback();
                    });
                }
                catch (Exception ex)
                {
                    isPatching = false;
                    Interface.GetMod().NextTick(() => Interface.GetMod().LogInfo("Exception while patching {0} assembly: {1}", ScriptName, ex.ToString()));
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
                Interface.GetMod().LogInfo("IOException while checking plugin: {0}", ScriptName);
            }
        }
    }
}
