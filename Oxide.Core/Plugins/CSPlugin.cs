using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

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

                for (var n = 0; n < args.Length; n++)
                {
                    if (args[n] == null && !CanAssignNull(Parameters[n].ParameterType))
                        return false;

                    if (args[n] == null) continue;

                    if (exact && !IsBaseHook)
                    {
                        if (args[n].GetType() != Parameters[n].ParameterType && args[n].GetType().MakeByRefType() != Parameters[n].ParameterType)
                            return false;
                    }
                    else
                    {
                        if (args[n].GetType().IsValueType)
                        {
                            if (!TypeDescriptor.GetConverter(Parameters[n].ParameterType).IsValid(args[n]))
                                return false;
                        }
                        else
                        {
                            if (!Parameters[n].ParameterType.IsInstanceOfType(args[n]))
                                return false;
                        }
                    }
                }

                return true;
            }

            private bool CanAssignNull(Type type)
            {
                if (!type.IsValueType) return true;
                return Nullable.GetUnderlyingType(type) != null;
            }
        }

        public class HookMatch
        {
            public HookMethod Method;

            public object[] Args;

            public HookMatch(HookMethod method, object[] args)
            {
                Method = method;
                Args = args;
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
            var received = args?.Length ?? 0;
            var matches = new List<HookMatch>();
            object return_value = null;

            // Find a method matching the hook signature
            if (!FindMatchingHook(name, args, matches, true))
                FindMatchingHook(name, args, matches);

            // Call all matching hooks
            foreach (var match in matches)
            {
                try
                {
                    // Call method with the correct number of arguments
                    return_value = InvokeMethod(match.Method, match.Args);
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }

                if (received != match.Method.Parameters.Length)
                    // A copy of the call arguments was used for this method call
                    for (var n = 0; n < match.Method.Parameters.Length; n++)
                        // Copy output values for out and by reference arguments back to the calling args
                        if (match.Method.Parameters[n].IsOut || match.Method.Parameters[n].ParameterType.IsByRef)
                            args[n] = match.Args[n];
            }

            return return_value;
        }

        protected bool FindMatchingHook(string name, object[] args, List<HookMatch> matches, bool exact = false)
        {
            object[] hook_args;
            List<HookMethod> methods;

            if (!hooks.TryGetValue(name, out methods)) return false;

            foreach (var method in methods.Except(matches.Select(x => x.Method)))
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
                }
                else
                    hook_args = args;

                if (!method.HasMatchingSignature(hook_args, exact)) continue;

                matches.Add(new HookMatch(method, hook_args));
            }

            return matches.Any(x => !x.Method.IsBaseHook);
        }

        protected virtual object InvokeMethod(HookMethod method, object[] args) => method.Method.Invoke(this, args);
    }
}
