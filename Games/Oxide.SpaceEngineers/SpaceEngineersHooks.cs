using Oxide.Core;
using Oxide.Core.Plugins;
using Sandbox;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SteamSDK;
using System;
using System.Linq;
using System.Text;
using VRage.Game.ModAPI;

namespace Oxide.Game.SpaceEngineers
{
    /// <summary>
    /// Game hooks and wrappers for the core Space Engineers plugin
    /// </summary>
    public partial class SpaceEngineersCore : CSPlugin
    {
        #region Server Hooks

        /// <summary>
        /// Called when the server is created sandbox
        /// </summary>
        [HookMethod("IOnSandboxCreated")]
        private void IOnSandboxCreated()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Utilities == null || MyAPIGateway.Multiplayer == null) return;

            MyAPIGateway.Multiplayer.RegisterMessageHandler(0xff20, OnChatMessageFromClient);
        }

        /// <summary>
        /// Called when the server is created sandbox
        /// </summary>
        [HookMethod("IOnNextTick")]
        private void IOnNextTick()
        {
            var deltaTime = MySandboxGame.TotalTimeInMilliseconds - m_totalTimeInMilliseconds / 1000.0f;
            m_totalTimeInMilliseconds = MySandboxGame.TotalTimeInMilliseconds;
            Interface.Oxide.OnFrame(deltaTime);
        }

        /// <summary>
        /// Called when the server logs append
        /// </summary>
        [HookMethod("OnWriteLine")]
        private void OnWriteLine(string message)
        {
            if (string.IsNullOrEmpty(message) || Filter.Any(message.StartsWith)) return;

            var color = ConsoleColor.Gray;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }

        /// <summary>
        /// Called when a chat message is received from a client
        /// </summary>
        /// <param name="bytes"></param>
        private void OnChatMessageFromClient(byte[] bytes)
        {
            var data = Encoding.Unicode.GetString(bytes);
            var args = data.Split(new char[] { ',' }, 2);
            var steamId = 0UL;
            if (ulong.TryParse(args[0], out steamId))
                IOnPlayerChat(steamId, args[1], ChatEntryTypeEnum.ChatMsg);
            else
                Interface.Oxide.LogError("Can't parse Steam ID...");
        }

        #endregion Server Hooks

        #region Player Hooks

        /// <summary>
        /// Called when the player sends a message
        /// </summary>
        /// <param name="steamId"></param>
        /// <param name="message"></param>
        [HookMethod("IOnPlayerChat")]
        private object IOnPlayerChat(ulong steamId, string message, ChatEntryTypeEnum chatType)
        {
            if (Sync.MyId == steamId || message.Trim().Length <= 1) return true;

            var str = message.Substring(0, 1);

            // Get covalence player
            var iplayer = Covalence.PlayerManager.FindPlayerById(steamId.ToString());
            var id = steamId.ToString();

            // Is it a chat command?
            if (!str.Equals("/"))
            {
                var chatSpecific = Interface.Call("OnPlayerChat", id, message);
                var chatCovalence = iplayer != null ? Interface.Call("OnUserChat", iplayer, message) : null;
                return chatSpecific ?? chatCovalence;
            }

            // Is this a covalence command?
            if (Covalence.CommandSystem.HandleChatMessage(iplayer, message)) return true;

            // Get the command string
            var command = message.Substring(1);

            // Parse it
            string cmd;
            string[] args;
            ParseCommand(command, out cmd, out args);
            if (cmd == null) return null;

            // Get session IMyPlayer
            var player = Player.Find(id);
            if (!cmdlib.HandleChatCommand(player, cmd, args))
            {
                Player.Reply(player, lang.GetMessage("UnknownCommand", this, player.SteamUserId.ToString()), cmd);
                return true;
            }

            // Call the game hook
            Interface.Call("OnChatCommand", id, command, args);

            return true;
        }

        /// <summary>
        /// Called when the player has connected
        /// </summary>
        /// <param name="myPlayer"></param>
        [HookMethod("IOnPlayerConnected")]
        private void IOnPlayerConnected(MyPlayer myPlayer)
        {
            var player = myPlayer as IMyPlayer;
            if (player == null) return;

            var id = player.SteamUserId.ToString();

            // Update player's permissions group and name
            if (permission.IsLoaded)
            {
                permission.UpdateNickname(id, player.DisplayName);
                var defaultGroups = Interface.Oxide.Config.Options.DefaultGroups;
                if (!permission.UserHasGroup(id, defaultGroups.Players)) permission.AddUserGroup(id, defaultGroups.Players);
                if (player.PromoteLevel == MyPromoteLevel.Admin && !permission.UserHasGroup(id, defaultGroups.Administrators))
                    permission.AddUserGroup(id, defaultGroups.Administrators);
            }

            Interface.Call("OnPlayerConnected", player);

            Covalence.PlayerManager.PlayerJoin(player.SteamUserId, player.DisplayName); // TODO: Move to OnUserApprove hook once available
            Covalence.PlayerManager.PlayerConnected(player);
            var iplayer = Covalence.PlayerManager.FindPlayerById(id);
            if (iplayer != null) Interface.Call("OnUserConnected", iplayer);
        }

        /// <summary>
        /// Called when the player has disconnected
        /// </summary>
        /// <param name="steamId"></param>
        [HookMethod("IOnPlayerDisconnected")]
        private void IOnPlayerDisconnected(ulong steamId)
        {
            var player = Player.FindById(steamId);
            if (player == null) return;

            Interface.Call("OnPlayerDisconnected", player);

            var iplayer = Covalence.PlayerManager.FindPlayerById(steamId.ToString());
            if (iplayer != null) Interface.Call("OnUserDisconnected", iplayer, "Unknown");
            Covalence.PlayerManager.PlayerDisconnected(player);

            Interface.Oxide.ServerConsole.AddMessage($" *  {player}", ConsoleColor.Yellow);
        }

        #endregion Player Hooks
    }
}
