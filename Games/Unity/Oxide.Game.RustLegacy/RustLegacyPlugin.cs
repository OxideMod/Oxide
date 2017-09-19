using System;
using System.Reflection;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.RustLegacy.Libraries;

namespace Oxide.Plugins
{
    public abstract class RustLegacyPlugin : CSharpPlugin
    {
        protected Command cmd = Interface.Oxide.GetLibrary<Command>();
        protected RustLegacy rust = Interface.Oxide.GetLibrary<RustLegacy>("Rust");

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (var field in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = field.GetCustomAttributes(typeof(OnlinePlayersAttribute), true);
                if (attributes.Length > 0)
                {
                    var plugin_field = new PluginFieldInfo(this, field);
                    if (plugin_field.GenericArguments.Length != 2 || plugin_field.GenericArguments[0] != typeof(NetUser))
                    {
                        Puts("The {0} field is not a Hash with a NetUser key! (online players will not be tracked)", field.Name);
                        continue;
                    }
                    if (!plugin_field.LookupMethod("Add", plugin_field.GenericArguments))
                    {
                        Puts("The {0} field does not support adding NetUser keys! (online players will not be tracked)", field.Name);
                        continue;
                    }
                    if (!plugin_field.LookupMethod("Remove", typeof(NetUser)))
                    {
                        Puts("The {0} field does not support removing NetUser keys! (online players will not be tracked)", field.Name);
                        continue;
                    }
                    if (plugin_field.GenericArguments[1].GetField("Player") == null)
                    {
                        Puts("The {0} class does not have a public Player field! (online players will not be tracked)", plugin_field.GenericArguments[1].Name);
                        continue;
                    }
                    onlinePlayerFields.Add(plugin_field);
                }
            }

            foreach (var method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = method.GetCustomAttributes(typeof(ConsoleCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ConsoleCommandAttribute;
                    if (attribute != null)  cmd.AddConsoleCommand(attribute.Command, this, method.Name);
                    continue;
                }

                attributes = method.GetCustomAttributes(typeof(ChatCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ChatCommandAttribute;
                    if (attribute != null) cmd.AddChatCommand(attribute.Command, this, method.Name);
                }
            }

            if (onlinePlayerFields.Count > 0) foreach (var playerClient in PlayerClient.All) AddOnlinePlayer(playerClient.netUser);

            base.HandleAddedToManager(manager);
        }

        [HookMethod("OnPlayerConnected")]
        private void base_OnPlayerInit(NetUser netUser) => AddOnlinePlayer(netUser);

        [HookMethod("OnPlayerDisconnected")]
        private void base_OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            // Delay removing player until OnPlayerDisconnect has fired in plugin
            if (player.GetLocalData() is NetUser)
            {
                NextTick(() =>
                {
                    foreach (var plugin_field in onlinePlayerFields) plugin_field.Call("Remove", player);
                });
            }
        }

        private void AddOnlinePlayer(NetUser player)
        {
            foreach (var plugin_field in onlinePlayerFields)
            {
                var type = plugin_field.GenericArguments[1];
                var online_player = type.GetConstructor(new[] { typeof(NetUser) }) == null ? Activator.CreateInstance(type) : Activator.CreateInstance(type, player);
                type.GetField("Player").SetValue(online_player, player);
                plugin_field.Call("Add", player, online_player);
            }
        }

        /// <summary>
        /// Print a message to the players console log
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToConsole(NetUser netUser, string format, params object[] args)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, "echo " + string.Format(format, args));
        }

        /// <summary>
        /// Print a message to every players console log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToConsole(string format, params object[] args)
        {
            if (PlayerClient.All.Count >= 1) ConsoleNetworker.Broadcast("echo " + string.Format(format, args));
        }

        /// <summary>
        /// Print a message to the players chat log
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(NetUser netUser, string format, params object[] args)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, "chat.add \"Server\" " + string.Format(format, args).Quote());
        }

        /// <summary>
        /// Print a message to every players chat log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(string format, params object[] args)
        {
            if (PlayerClient.All.Count < 1) return;
            ConsoleNetworker.Broadcast("chat.add \"Server\"" + string.Format(format, args).Quote());
        }

        /// <summary>
        /// Send a reply message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void SendReply(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            var message = string.Format(format, args);
            if (arg.argUser != null)
            {
                PrintToConsole(arg.argUser, format, args);
                return;
            }
            Puts(message);
        }

        /// <summary>
        /// Send a reply message in response to a chat command
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void SendReply(NetUser netUser, string format, params object[] args) => PrintToChat(netUser, format, args);

        /// <summary>
        /// Send a warning message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void SendWarning(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            var message = string.Format(format, args);
            if (arg.argUser != null)
            {
                rust.SendConsoleMessage(arg.argUser, format, args);
                return;
            }
            Debug.LogWarning(message);
        }

        /// <summary>
        /// Send an error message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void SendError(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            var message = string.Format(format, args);
            if (arg.argUser != null)
            {
                rust.SendConsoleMessage(arg.argUser, format, args);
                return;
            }
            Debug.LogError(message);
        }
    }
}
