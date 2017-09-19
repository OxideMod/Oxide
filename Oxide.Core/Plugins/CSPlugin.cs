using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Oxide.Core.Libraries;

namespace Oxide.Core.Plugins
{
    /// <summary>
    /// Indicates that the specified method should be a handler for a hook
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class HookMethodAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the hook to... hook
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the HookMethod class
        /// </summary>
        /// <param name="name"></param>
        public HookMethodAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Represents a plugin implemented in .NET
    /// </summary>
    public abstract class CSPlugin : Plugin
    {
        public class HookMethod
        {
            public MethodInfo Method;
            public string Name;
            public ParameterInfo[] Parameters;
            public bool IsBaseHook;

            public HookMethod(MethodInfo method)
            {
                Method = method;
                Name = method.Name;
                Parameters = method.GetParameters();
                IsBaseHook = method.Name.StartsWith("base_");

                if (Parameters.Length > 0)
                    Name += $"({string.Join(", ", Parameters.Select(x => x.ParameterType.ToString()).ToArray())})";
            }

            public bool HasMatchingSignature(object[] args, bool exact = false)
            {
                if (Parameters.Length == 0 && (args == null || args.Length == 0))
                    return true;

                // Check if hook signature matches
                for (var n = 0; n < args.Length; n++)
                {
                    if (exact)
                    {
                        if (args[n].GetType() != Parameters[n].ParameterType)
                            return false;
                    }
                    else
                    {
                        if (!Parameters[n].ParameterType.IsAssignableFrom(args[n].GetType()))
                            return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the library by the specified type or name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetLibrary<T>(string name = null) where T : Library => Interface.Oxide.GetLibrary<T>(name);

        // All hooked methods
        protected Dictionary<string, List<HookMethod>> hooks = new Dictionary<string, List<HookMethod>>();

        /// <summary>
        /// Initializes a new instance of the CSPlugin class
        /// </summary>
        public CSPlugin()
        {
            // Find all hooks in the plugin and any base classes derived from CSPlugin
            var types = new List<Type>();
            var type = GetType();
            types.Add(type);
            while (type != typeof(CSPlugin)) types.Add(type = type.BaseType);

            // Add hooks implemented in base classes before user implemented methods
            for (var i = types.Count - 1; i >= 0; i--)
            {
                foreach (var method in types[i].GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
                {
                    var attr = method.GetCustomAttributes(typeof(HookMethodAttribute), true);
                    if (attr.Length < 1) continue;
                    var hookmethod = attr[0] as HookMethodAttribute;
                    AddHookMethod(hookmethod?.Name, method);
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
            foreach (var hookname in hooks.Keys) Subscribe(hookname);

            try
            {
                // Let the plugin know that it's loading
                OnCallHook("Init", null);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"Failed to initialize plugin '{Name} v{Version}'", ex);
                if (Loader != null) Loader.PluginErrors[Name] = ex.Message;
            }
        }

        protected void AddHookMethod(string name, MethodInfo method)
        {
            List<HookMethod> hook_methods;
            if (!hooks.TryGetValue(name, out hook_methods))
            {
                hook_methods = new List<HookMethod>();
                hooks[name] = hook_methods;
            }
            hook_methods.Add(new HookMethod(method));
        }

        /// <summary>
        /// Calls the specified hook on this plugin
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected sealed override object OnCallHook(string name, object[] args)
        {
            List<HookMethod> methods;
            if (!hooks.TryGetValue(name, out methods)) return null;

            HookMethod method;
            object[] hook_args;
            var received = args?.Length ?? 0;
            object return_value;

            // Find a method matching the hook signature
            if (!FindMatchingHook(methods, args, out method, out hook_args, true))
                if (!FindMatchingHook(methods, args, out method, out hook_args))
                    // No matching hook found on this plugin
                    return null;

            try
            {
                // Call method with the correct number of arguments
                return_value = InvokeMethod(method, hook_args);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }

            if (received != method.Parameters.Length)
                // A copy of the call arguments was used for this method call
                for (var n = 0; n < method.Parameters.Length; n++)
                    // Copy output values for out and by reference arguments back to the calling args
                    if (method.Parameters[n].IsOut || method.Parameters[n].ParameterType.IsByRef)
                        args[n] = hook_args[n];

            return return_value;
        }

        protected virtual object InvokeMethod(HookMethod method, object[] args) => method.Method.Invoke(this, args);

        protected bool FindMatchingHook(List<HookMethod> methods, object[] args, out HookMethod match, out object[] hook_args, bool exact = false)
        {
            match = null;
            hook_args = args;

            foreach (var method in methods)
            {
                var received = args?.Length ?? 0;

                if (received != method.Parameters.Length)
                {
                    // The call argument count is different to the declared callback methods argument count
                    hook_args = new object[method.Parameters.Length];

                    if (received > 0 && hook_args.Length > 0)
                        // Remove any additional arguments which the callback method does not declare
                        Array.Copy(args, hook_args, Math.Min(received, hook_args.Length));

                    if (hook_args.Length > received)
                    {
                        // Create additional parameters for arguments excluded in this hook call
                        for (var n = received; n < hook_args.Length; n++)
                        {
                            var parameter = method.Parameters[n];
                            if (parameter.DefaultValue != null && parameter.DefaultValue != DBNull.Value)
                                // Use the default value that was provided by the method definition
                                hook_args[n] = parameter.DefaultValue;
                            else if (parameter.ParameterType.IsValueType)
                                // Use the default value for value types
                                hook_args[n] = Activator.CreateInstance(parameter.ParameterType);
                        }
                    }
                } else
                    hook_args = args;

                if (!method.HasMatchingSignature(hook_args, exact)) continue;

                match = method;
                return true;
            }

            return false;
        }

    }
}
