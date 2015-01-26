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
        /// Initialises a new instance of the HookMethod class
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
        protected IDictionary<string, MethodInfo> hooks;

        /// <summary>
        /// Initialises a new instance of the CSPlugin class
        /// </summary>
        public CSPlugin()
        {
            // Initialise
            hooks = new Dictionary<string, MethodInfo>();

            // Find all hooks
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attr = method.GetCustomAttributes(typeof(HookMethod), true);
                if (attr.Length > 0)
                {
                    HookMethod hookmethod = attr[0] as HookMethod;
                    hooks.Add(hookmethod.Name, method);
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
            OnCallHook("Init", null);
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
            MethodInfo method;
            if (!hooks.TryGetValue(hookname, out method)) return null;

            // Verify the args
            ParameterInfo[] parameters = method.GetParameters();
            object[] funcargs = new object[parameters.Length];
            int args_received;
            if (args == null)
                args_received = 0;
            else
            {
                args_received = args.Length;
                Array.Copy(args, funcargs, Math.Min(args_received, funcargs.Length));
            }
            if (funcargs.Length > args_received)
            {
                // Invent args in an attempt to let the invoke call work
                for (int i = args_received; i < funcargs.Length; i++)
                {
                    ParameterInfo pinfo = parameters[i];

                    // Does it have a default value? Fill it in
                    //if (pinfo.DefaultValue != null)
                        //funcargs[i] = pinfo.DefaultValue;
                    // Is it a value type? Pass in the default
                    if (pinfo.ParameterType.IsValueType)
                        funcargs[i] = Activator.CreateInstance(pinfo.ParameterType);
                    // Otherwise it's a reference type so just leaving it null will work
                }
            }

            // Call it
            try
            {
                return method.Invoke(this, funcargs);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }
    }
}
