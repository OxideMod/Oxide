using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Core.Plugins
{
    /// <summary>
    /// Indicates that the specified method should be a handler for a hook
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class HookMethod : Attribute
    {
        /// <summary>
        /// Gets the name of the hook to... hook
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initializes a new instance of the HookMethod class
        /// </summary>
        /// <param name="name"></param>
        public HookMethod(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Represents a plugin implemented in .NET
    /// </summary>
    public abstract class CSPlugin : Plugin
    {
        // All hooked methods
        protected IDictionary<string, List<MethodInfo>> hooks;

        /// <summary>
        /// Initializes a new instance of the CSPlugin class
        /// </summary>
        public CSPlugin()
        {
            // Initialize
            hooks = new Dictionary<string, List<MethodInfo>>();

            // Find all hooks in the plugin and any base classes derived from CSPlugin
            var types = new List<Type>();
            var type = GetType();
            types.Add(type);
            while (type != typeof(CSPlugin)) types.Add(type = type.BaseType);

            // Add hooks implemented in base classes before user implemented methods
            for (var i = types.Count - 1; i >= 0; i--)
            {
                foreach (var method in types[i].GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    var attr = method.GetCustomAttributes(typeof(HookMethod), true);
                    if (attr.Length < 1) continue;
                    var hookmethod = attr[0] as HookMethod;
                    AddHookMethod(hookmethod.Name, method);
                }
            }
        }

        /// <summary>
        /// Called when this plugin has been added to a manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleAddedToManager(PluginManager manager)
        {
            // Let base work
            base.HandleAddedToManager(manager);

            // Subscribe us
            foreach (string hookname in hooks.Keys)
                Subscribe(hookname);

            // Let the plugin know that it's loading
            CallHook("Init", null);
        }

        protected void AddHookMethod(string name, MethodInfo method)
        {
            List<MethodInfo> hook_methods;
            if (!hooks.TryGetValue(name, out hook_methods))
            {
                hook_methods = new List<MethodInfo>();
                hooks[name] = hook_methods;
            }
            hook_methods.Add(method);
        }

        /// <summary>
        /// Calls the specified hook on this plugin
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override object OnCallHook(string name, object[] args)
        {
            List<MethodInfo> methods;
            if (!hooks.TryGetValue(name, out methods)) return null;
            
            object return_value = null;
            foreach (var method in methods)
            {
                var hook_args = args;
                var parameters = method.GetParameters();
                var args_received = args?.Length ?? 0;

                if (args_received != parameters.Length)
                {
                    // The call argument count is different to the declared callback methods argument count
                    hook_args = new object[parameters.Length];
                    
                    if (args_received > 0)
                    {
                        // Remove any additional arguments which the callback method does not declare
                        Array.Copy(args, hook_args, Math.Min(args_received, hook_args.Length));
                    }
                    if (hook_args.Length > args_received)
                    {
                        // Create additional parameters for arguments excluded in this hook call
                        for (var i = args_received; i < hook_args.Length; i++)
                        {
                            var parameter = parameters[i];
                            // Use the default value if one is provided by the method definition or the argument is a value type
                            if (parameter.DefaultValue != null || parameter.ParameterType.IsValueType)
                                hook_args[i] = parameter.DefaultValue ?? Activator.CreateInstance(parameter.ParameterType);
                        }
                    }
                }

                try
                {
                    // Call method with the correct number of arguments
                    var value = method.Invoke(this, hook_args);
                    if (value != null) return_value = value;
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }

                if (args_received != parameters.Length)
                {
                    // A copy of the call arguments was used for this method call
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        // Copy output values for out and by reference arguments back to the calling args
                        if (parameter.IsOut || parameter.ParameterType.IsByRef)
                            args[i] = hook_args[i];
                    }
                }
            }
            
            return return_value;
        }
    }
}
