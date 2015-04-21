﻿using System;
using System.Reflection;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using Oxide.RustLegacy.Libraries;

using UnityEngine;

namespace Oxide.Plugins
{
    public abstract class RustLegacyPlugin : CSharpPlugin
    {
        protected Command cmd;
        protected Permission permission;
        protected RustLegacy.Libraries.RustLegacy rust;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            cmd = Interface.Oxide.GetLibrary<Command>("Command");
            permission = Interface.Oxide.GetLibrary<Permission>("Permission");
            rust = Interface.Oxide.GetLibrary<RustLegacy.Libraries.RustLegacy>("Rust");
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (FieldInfo field in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = field.GetCustomAttributes(typeof(OnlinePlayersAttribute), true);
                if (attributes.Length > 0)
                {
                    var plugin_field = new PluginFieldInfo(this, field);
                    if (plugin_field.GenericArguments.Length != 2 || plugin_field.GenericArguments[0] != typeof(NetUser))
                    {
                        Puts("[{0}] The {1} field is not a Hash with a NetUser key! (online players will not be tracked)", Name, field.Name);
                        continue;
                    }
                    if (!plugin_field.LookupMethod("Add", plugin_field.GenericArguments))
                    {
                        Puts("[{0}] The {1} field does not support adding NetUser keys! (online players will not be tracked)", Name, field.Name);
                        continue;
                    }
                    if (!plugin_field.LookupMethod("Remove", typeof(NetUser)))
                    {
                        Puts("[{0}] The {1} field does not support removing NetUser keys! (online players will not be tracked)", Name, field.Name);
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
                foreach (var playerClient in PlayerClient.All)
                    AddOnlinePlayer(playerClient.netUser);
            }

            base.HandleAddedToManager(manager);
        }

        [HookMethod("OnPlayerConnected")]
        private void base_OnPlayerInit(NetUser player)
        {
            AddOnlinePlayer(player);
        }

        [HookMethod("OnPlayerDisconnected")]
        private void base_OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            // Delay removing player until OnPlayerDisconnect has fired in plugin
            if (player.GetLocalData() is NetUser)
            {
                NextTick(() =>
                {
                    foreach (var plugin_field in onlinePlayerFields)
                        plugin_field.Call("Remove", player);
                });
            }
        }

        private void AddOnlinePlayer(NetUser player)
        {
            foreach (var plugin_field in onlinePlayerFields)
            {
                var type = plugin_field.GenericArguments[1];
                object online_player;
                if (type.GetConstructor(new Type[] { typeof(NetUser) }) == null)
                    online_player = Activator.CreateInstance(type);
                else
                    online_player = Activator.CreateInstance(type, (object)player);
                type.GetField("Player").SetValue(online_player, player);
                plugin_field.Call("Add", player, online_player);
            }
        }

        // <summary>
        // Send a reply message in response to a console command
        // </summary>
        // <param name="arg"></param>
        // <param name="format"></param>
        // <param name="args"></param>
        protected void SendReply(ConsoleSystem.Arg arg, string format, params object[] args)
        {
            var message = string.Format(format, args);

            if (arg.argUser != null)
            {
                rust.SendConsoleMessage(arg.argUser, format, args);
                return;
            }

            Puts(message);
        }

        // <summary>
        // Send a reply message in response to a chat command
        // </summary>
        // <param name="player"></param>
        // <param name="message"></param>
        protected void SendReply(NetUser player, string message)
        {
            SendReply(player, "Oxide", message);
        }

        protected void SendReply(NetUser player, string name, string message)
        {
            rust.SendChatMessage(player, name, message);
        }

        // <summary>
        // Send a warning message in response to a console command
        // </summary>
        // <param name="arg"></param>
        // <param name="format"></param>
        // <param name="args"></param>
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

        // <summary>
        // Send an error message in response to a console command
        // </summary>
        // <param name="arg"></param>
        // <param name="format"></param>
        // <param name="args"></param>
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
