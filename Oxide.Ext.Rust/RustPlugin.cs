﻿using System;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using Oxide.Rust.Libraries;

using UnityEngine;
using Network;

namespace Oxide.Plugins
{
    public abstract class RustPlugin : CSharpPlugin
    {
        protected Command cmd;
        protected Permission permission;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            cmd = Interface.GetMod().GetLibrary<Command>("Command");
            permission = Interface.GetMod().GetLibrary<Permission>("Permission");
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (FieldInfo field in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = field.GetCustomAttributes(typeof(OnlinePlayersAttribute), true);
                if (attributes.Length > 0)
                {
                    var plugin_field = new PluginFieldInfo(this, field);
                    if (plugin_field.GenericArguments.Length != 2 || plugin_field.GenericArguments[0] != typeof(BasePlayer))
                    {
                        Puts("[{0}] The {1} field is not a Hash with a BasePlayer key! (online players will not be tracked)", Name, field.Name);
                        continue;
                    }
                    if (!plugin_field.LookupMethod("Add", plugin_field.GenericArguments))
                    {
                        Puts("[{0}] The {1} field does not support adding BasePlayer keys! (online players will not be tracked)", Name, field.Name);
                        continue;
                    }
                    if (!plugin_field.LookupMethod("Remove", typeof(BasePlayer)))
                    {
                        Puts("[{0}] The {1} field does not support removing BasePlayer keys! (online players will not be tracked)", Name, field.Name);
                        continue;
                    }
                    if (plugin_field.GenericArguments[1].GetField("Player") == null)
                    {
                        Puts("[{0}] The {1} class does not have a public Player field! (online players will not be tracked)", Name, plugin_field.GenericArguments[1].Name);
                        continue;
                    }
                    onlinePlayerFields.Add(plugin_field);
                }
            }

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

            if (onlinePlayerFields.Count > 0)
            {
                foreach (var player in BasePlayer.activePlayerList)
                    AddOnlinePlayer(player);
            }

            base.HandleAddedToManager(manager);
        }

        [HookMethod("OnPlayerInit")]
        private void base_OnPlayerInit(BasePlayer player)
        {
            AddOnlinePlayer(player);
        }
        
        [HookMethod("OnUserApprove")]
        private object base_OnUserApprove(Network.Connection connection)
        {
            if (ServerUsers.Is(connection.ownerid, ServerUsers.UserGroup.Banned))
            {
                ConnectionAuth.Reject(connection, "You are banned from this server");
                return false;
            }
        }

        [HookMethod("OnPlayerDisconnected")]
        private void base_OnPlayerDisconnected(BasePlayer player)
        {
            // Delay removing player until OnPlayerDisconnect has fired in plugin
            NextTick(() =>
            {
                foreach (var plugin_field in onlinePlayerFields)
                    plugin_field.Call("Remove", player);
            });
        }

        private void AddOnlinePlayer(BasePlayer player)
        {
            foreach (var plugin_field in onlinePlayerFields)
            {
                var type = plugin_field.GenericArguments[1];
                object online_player;
                if (type.GetConstructor(new Type[] { typeof(BasePlayer) }) == null)
                    online_player = Activator.CreateInstance(type);
                else
                    online_player = Activator.CreateInstance(type, (object)player);
                type.GetField("Player").SetValue(online_player, player);
                plugin_field.Call("Add", player, online_player);
            }
        }

        /// <summary>
        /// Print a message to a players console log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToConsole(BasePlayer player, string format, params object[] args)
        {
            player.SendConsoleCommand("echo " + string.Format(format, args), new object[0]);
        }

        /// <summary>
        /// Print a message to every players console log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToConsole(string format, params object[] args)
        {
            if (BasePlayer.activePlayerList.Count < 1) return;
            ConsoleSystem.Broadcast("echo " + string.Format(format, args), new object[0]);
        }

        /// <summary>
        /// Print a message to a players chat log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(BasePlayer player, string format, params object[] args)
        {
            player.SendConsoleCommand("chat.add", 0, string.Format(format, args), 1f);
        }

        /// <summary>
        /// Print a message to every players chat log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(string format, params object[] args)
        {
            if (BasePlayer.activePlayerList.Count < 1) return;
            ConsoleSystem.Broadcast("chat.add", 0, string.Format(format, args), 1f);
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
        /// <param name="args"></param>
        protected void SendReply(BasePlayer player, string format, params object[] args)
        {
            PrintToChat(player, format, args);
        }

        /// <summary>
        /// Send a warning message in response to a console command
        /// </summary>
        /// <param name="arg"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
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
        /// <param name="args"></param>
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

        /// <summary>
        /// Forces a player to a specific position
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        protected void ForcePlayerPosition(BasePlayer player, Vector3 destination)
        {
            player.transform.position = destination;
            if (!player.IsSpectating() || Vector3.Distance(player.transform.position, destination) > 25.0)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
            else
                player.SendNetworkUpdate(BasePlayer.NetworkQueue.UpdateDistance);
        }
    }
}
