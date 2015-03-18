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
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected override object OnCallHook(string hookname, object[] args)
        {
            // Get the method
            List<MethodInfo> methods;
            if (!hooks.TryGetValue(hookname, out methods)) return null;

            object return_value = null;
            foreach (var method in methods)
            {
                // Call it
                try
                {
                    var value = method.Invoke(this, args);
                    if (value != null) return_value = value;
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }

            return return_value;
        }
    }
}
