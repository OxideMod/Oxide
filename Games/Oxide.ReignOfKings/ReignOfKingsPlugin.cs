using CodeHatch.Common;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Players;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.ReignOfKings.Libraries;
using System;
using System.Reflection;

namespace Oxide.Plugins
{
    public abstract class ReignOfKingsPlugin : CSharpPlugin
    {
        protected Command cmd = Interface.Oxide.GetLibrary<Command>();
        protected ReignOfKings rok = Interface.Oxide.GetLibrary<ReignOfKings>("RoK");

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (var field in GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = field.GetCustomAttributes(typeof(OnlinePlayersAttribute), true);
                if (attributes.Length > 0)
                {
                    var pluginField = new PluginFieldInfo(this, field);
                    if (pluginField.GenericArguments.Length != 2 || pluginField.GenericArguments[0] != typeof(Player))
                    {
                        Puts($"The {field.Name} field is not a Hash with the player key! (online players will not be tracked)");
                        continue;
                    }
                    if (!pluginField.LookupMethod("Add", pluginField.GenericArguments))
                    {
                        Puts($"The {field.Name} field does not support adding Player keys! (online players will not be tracked)");
                        continue;
                    }
                    if (!pluginField.LookupMethod("Remove", typeof(Player)))
                    {
                        Puts($"The {field.Name} field does not support removing Player keys! (online players will not be tracked)");
                        continue;
                    }
                    if (pluginField.GenericArguments[1].GetField("Player") == null)
                    {
                        Puts($"The {pluginField.GenericArguments[1].Name} class does not have a public Player field! (online players will not be tracked)");
                        continue;
                    }
                    onlinePlayerFields.Add(pluginField);
                }
            }

            foreach (var method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = method.GetCustomAttributes(typeof(ChatCommandAttribute), true);
                if (attributes.Length <= 0) continue;
                var attribute = attributes[0] as ChatCommandAttribute;
                cmd.AddChatCommand(attribute?.Command, this, method.Name);
            }

            base.HandleAddedToManager(manager);
        }
        [HookMethod("OnPlayerSpawn")]
        private void base_OnPlayerSpawn(PlayerFirstSpawnEvent e) => AddOnlinePlayer(e.Player);

        [HookMethod("OnPlayerDisconnected")]
        private void base_OnPlayerDisconnected(Player player)
        {
            // Delay removing player until OnPlayerDisconnect has fired in plugin
            NextTick(() =>
            {
                foreach (var pluginField in onlinePlayerFields) pluginField.Call("Remove", player);
            });
        }

        private void AddOnlinePlayer(Player player)
        {
            foreach (var pluginField in onlinePlayerFields)
            {
                var type = pluginField.GenericArguments[1];
                var onlinePlayer = type.GetConstructor(new[] { typeof(Player) }) == null ? Activator.CreateInstance(type) : Activator.CreateInstance(type, player);
                type.GetField("Player").SetValue(onlinePlayer, player);
                pluginField.Call("Add", player, onlinePlayer);
            }
        }

        /// <summary>
        /// Print a message to the players chat log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(Player player, string format, params object[] args) => player.SendMessage(format, args);

        /// <summary>
        /// Print a message to every players chat log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(string format, params object[] args)
        {
            if (Server.PlayerCount >= 1) Server.BroadcastMessage(format, args);
        }

        /// <summary>
        /// Send a reply message in response to a chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void SendReply(Player player, string format, params object[] args) => PrintToChat(player, format, args);
    }
}
