using System;
using System.Reflection;

using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;

using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

using Oxide.Game.ReignOfKings.Libraries;

namespace Oxide.Plugins
{
    public abstract class ReignOfKingsPlugin : CSharpPlugin
    {
        protected Command cmd;
        protected Permission permission;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            cmd = Interface.Oxide.GetLibrary<Command>("Command");
            permission = Interface.Oxide.GetLibrary<Permission>("Permission");
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (FieldInfo field in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = field.GetCustomAttributes(typeof(OnlinePlayersAttribute), true);
                if (attributes.Length > 0)
                {
                    var plugin_field = new PluginFieldInfo(this, field);
                    if (plugin_field.GenericArguments.Length != 2 || plugin_field.GenericArguments[0] != typeof(Player))
                    {
                        Puts("[{0}] The {1} field is not a Hash with a Player key! (online players will not be tracked)", Name, field.Name);
                        continue;
                    }
                    if (!plugin_field.LookupMethod("Add", plugin_field.GenericArguments))
                    {
                        Puts("[{0}] The {1} field does not support adding Player keys! (online players will not be tracked)", Name, field.Name);
                        continue;
                    }
                    if (!plugin_field.LookupMethod("Remove", typeof(Player)))
                    {
                        Puts("[{0}] The {1} field does not support removing Player keys! (online players will not be tracked)", Name, field.Name);
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

            foreach (var method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = method.GetCustomAttributes(typeof(ChatCommandAttribute), true);
                if (attributes.Length <= 0) continue;
                var attribute = attributes[0] as ChatCommandAttribute;
                cmd.AddChatCommand(attribute.Command, this, method.Name);
            }

            base.HandleAddedToManager(manager);
        }
        [HookMethod("OnPlayerSpawn")]
        private void base_OnPlayerSpawn(PlayerFirstSpawnEvent e)
        {
            AddOnlinePlayer(e.Player);
        }

        [HookMethod("OnPlayerDisconnected")]
        private void base_OnPlayerDisconnected(Player player)
        {
            // Delay removing player until OnPlayerDisconnect has fired in plugin
            NextTick(() =>
            {
                foreach (var plugin_field in onlinePlayerFields)
                    plugin_field.Call("Remove", player);
            });
        }

        private void AddOnlinePlayer(Player player)
        {
            foreach (var plugin_field in onlinePlayerFields)
            {
                var type = plugin_field.GenericArguments[1];
                object online_player;
                if (type.GetConstructor(new[] { typeof(Player) }) == null)
                    online_player = Activator.CreateInstance(type);
                else
                    online_player = Activator.CreateInstance(type, (object)player);
                type.GetField("Player").SetValue(online_player, player);
                plugin_field.Call("Add", player, online_player);
            }
        }

        /// <summary>
        /// Print a message to a players chat log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(Player player, string format, params object[] args)
        {
            player.SendMessage(format, args);
        }

        /// <summary>
        /// Print a message to every players chat log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(string format, params object[] args)
        {
            if (Server.PlayerCount < 1) return;
            Server.BroadcastMessage(format, args);
        }

        /// <summary>
        /// Send a reply message in response to a chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void SendReply(Player player, string format, params object[] args)
        {
            PrintToChat(player, format, args);
        }
    }
}
