using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Reflection;

namespace Oxide.Plugins
{
    /// <summary>
    /// Indicates that the specified method should be a handler for a covalence command
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public string[] Commands { get; }

        public CommandAttribute(params string[] commands)
        {
            Commands = commands;
        }
    }

    /// <summary>
    /// Indicates that the specified method requires a specific permission
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PermissionAttribute : Attribute
    {
        public string[] Permission { get; }

        public PermissionAttribute(string permission)
        {
            Permission = new[] { permission };
        }
    }

    public class CovalencePlugin : CSharpPlugin
    {
        private new static readonly Covalence covalence = Interface.Oxide.GetLibrary<Covalence>();

        protected string game = covalence.Game;
        protected IPlayerManager players = covalence.Players;
        protected IServer server = covalence.Server;

        /// <summary>
        /// Print an info message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void Log(string format, params object[] args)
        {
            Interface.Oxide.LogInfo("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print a warning message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarning(string format, params object[] args)
        {
            Interface.Oxide.LogWarning("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print an error message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogError(string format, params object[] args)
        {
            Interface.Oxide.LogError("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Called when this plugin has been added to the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (var method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var commandAttribute = method.GetCustomAttributes(typeof(CommandAttribute), true);
                var permissionAttribute = method.GetCustomAttributes(typeof(PermissionAttribute), true);
                if (commandAttribute.Length <= 0) continue;

                var cmd = commandAttribute[0] as CommandAttribute;
                var perm = permissionAttribute.Length <= 0 ? null : permissionAttribute[0] as PermissionAttribute;
                if (cmd == null) continue;
                AddCovalenceCommand(cmd.Commands, perm?.Permission, (caller, command, args) =>
                {
                    CallHook(method.Name, caller, command, args);
                    return true;
                });
            }

            base.HandleAddedToManager(manager);
        }
    }
}
