using System;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Rust.Libraries;

using UnityEngine;

namespace Oxide.Plugins
{
    public abstract class RustPlugin : CSharpPlugin
    {
        protected Command cmd;
        protected Permissions permissions;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            cmd = Interface.GetMod().GetLibrary<Command>("Command");
            permissions = Interface.GetMod().GetLibrary<Permissions>("Permissions");
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = method.GetCustomAttributes(typeof(ConsoleCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ConsoleCommandAttribute;
                    cmd.AddConsoleCommand(attribute.Command, this, method.Name);
                    continue;
                }

                attributes = method.GetCustomAttributes(typeof(ChatCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ChatCommandAttribute;
                    cmd.AddChatCommand(attribute.Command, this, method.Name);
                }
            }

            base.HandleAddedToManager(manager);
        }

        /// <summary>
        /// Print a message to a players console log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void PrintToConsole(BasePlayer player, string format, params object[] args)
        {
            player.SendConsoleCommand("echo " + string.Format(format, args));
        }

        /// <summary>
        /// Print a message to a players chat log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void PrintToChat(BasePlayer player, string format, params object[] args)
        {
            player.SendConsoleCommand("chat.add \"Oxide\" " + StringExtensions.QuoteSafe(string.Format(format, args)));
        }

        /// <summary>
        /// Send a reply message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void SendReply(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            var message = string.Format(format, args);

            if (arg.connection != null)
            {
                var player = arg.connection.player as BasePlayer;
                if (player != null)
                {
                    player.SendConsoleCommand("echo " + message);
                    return;
                }
            }

            Puts(message);
        }

        /// <summary>
        /// Send a reply message in response to a chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void SendReply(BasePlayer player, string format, params object[] args)
        {
            PrintToChat(player, format, args);
        }

        /// <summary>
        /// Send a warning message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void SendWarning(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            var message = string.Format(format, args);

            if (arg.connection != null)
            {
                var player = arg.connection.player as BasePlayer;
                if (player != null)
                {
                    player.SendConsoleCommand("echo " + message);
                    return;
                }
            }

            Debug.LogWarning(message);
        }

        /// <summary>
        /// Send an error message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="params"></param>
        protected void SendError(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            var message = string.Format(format, args);

            if (arg.connection != null)
            {
                var player = arg.connection.player as BasePlayer;
                if (player != null)
                {
                    player.SendConsoleCommand("echo " + message);
                    return;
                }
            }

            Debug.LogError(message);
        }
    }
}