using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Sandbox;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Network;
using VRageMath;

namespace Oxide.Game.MedievalEngineers.Libraries
{
    public class Player : Library
    {
        #region Information

        /// <summary>
        /// Gets the player's language
        /// </summary>
        public CultureInfo Language(IMyPlayer player) => CultureInfo.GetCultureInfo("en"); // TODO: Implement when possible

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        public string Address(IMyPlayer player) => "0"; // TODO: Implement when possible

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        public int Ping(IMyPlayer player) => 0; // TODO: Implement when possible

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin(string id) => IsAdmin(Convert.ToUInt64(id));

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin(ulong id) => MySession.Static.HasPlayerAdminRights(id);

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin(IMyPlayer player) => player.IsAdmin;

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned(string id) => IsBanned(Convert.ToUInt64(id));

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned(ulong id) => MySandboxGame.ConfigDedicated.Banned.Contains(id);

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned(IMyPlayer player) => IsBanned(player.SteamUserId);

        /// <summary>
        /// Gets if the player is connected
        /// </summary>
        public bool IsConnected(IMyPlayer player) => Sync.Clients.HasClient(player.SteamUserId);

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping(string id) => IsSleeping(Convert.ToUInt64(id));

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping(ulong id) => false; // TODO: Implement if possible

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping(IMyPlayer player) => IsSleeping(player.SteamUserId);

        #endregion Information

        #region Administration

        /// <summary>
        /// Bans the player from the server
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        public void Ban(IMyPlayer player, string reason = "")
        {
            if (!IsBanned(player)) MyMultiplayer.Static.BanClient(player.SteamUserId, true);
        }

        /// <summary>
        /// Heals the player by specified amount
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        public void Heal(IMyPlayer player, float amount) => (player as MyPlayer).Character.StatComp.Health.Increase(amount, null);

        /// <summary>
        /// Damages the player by specified amount
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        public void Hurt(IMyPlayer player, float amount) => (player as MyPlayer).Character.DoDamage(amount, MyDamageType.Unknown, true);

        /// <summary>
        /// Kicks the player from the server
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        public void Kick(IMyPlayer player, string reason = "") => MyMultiplayer.Static.KickClient(player.SteamUserId);

        /// <summary>
        /// Causes the player to die
        /// </summary>
        /// <param name="player"></param>
        public void Kill(IMyPlayer player)
        {
            var damageInformation = new MyDamageInformation(true, 1f, MyDamageType.Deformation, (long)Sync.ServerId);
            (player as MyPlayer).Character.Kill(true, damageInformation);
        }

        /// <summary>
        /// Renames the player to specified name
        /// <param name="player"></param>
        /// <param name="name"></param>
        /// </summary>
        public void Rename(IMyPlayer player, string name)
        {
            // TODO: Implement when possible
        }

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        public void Teleport(IMyPlayer player, Vector3D destination) => player.Controller.ControlledEntity.Entity.PositionComp.SetPosition(destination);

        /// <summary>
        /// Teleports the player to the target player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        public void Teleport(IMyPlayer player, IMyPlayer target) => Teleport(player, Position(target));

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="player"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(IMyPlayer player, float x, float y, float z) => Teleport(player, new Vector3D(x, y, z));

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban(IMyPlayer player)
        {
            if (IsBanned(player)) MyMultiplayer.Static.BanClient(player.SteamUserId, false);
        }

        #endregion Administration

        #region Location

        /// <summary>
        /// Returns the position of player as Vector3
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Vector3D Position(IMyPlayer player) => player.GetPosition();

        #endregion Location

        #region Player Finding

        /// <summary>
        /// Gets the player object using a name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public IMyPlayer Find(string nameOrIdOrIp)
        {
            IMyPlayer player = null;
            foreach (var p in Players)
            {
                if (!nameOrIdOrIp.Equals(p.DisplayName, StringComparison.OrdinalIgnoreCase) &&
                    !nameOrIdOrIp.Equals(p.SteamUserId.ToString())) continue;
                player = p;
                break;
            }
            return player;
        }

        // TODO: Find player by objects

        /// <summary>
        /// Gets the player object using a Steam ID
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        public IMyPlayer FindById(ulong steamId)
        {
            //MyPlayerCollection.GetPlayerById(steamId);
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p.SteamUserId == steamId);
            return players.Count == 0 ? null : players[0];
        }

        /// <summary>
        /// Returns all connected players
        /// </summary>
        public List<IMyPlayer> Players
        {
            get
            {
                var players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);
                return players;
            }
        }

        #endregion Player Finding

        #region Chat and Commands

        /// <summary>
        /// Runs the specified player command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(IMyPlayer player, string command, params object[] args)
        {
            // TODO: Implement when possible
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Message(IMyPlayer player, string message, string prefix = null)
        {
            if (string.IsNullOrEmpty(message)) return;

            message = Formatter.ToPlaintext(message);
            var msg = new ChatMsg
            {
                Text = string.IsNullOrEmpty(prefix) ? message : (string.IsNullOrEmpty(message) ? prefix : $"{prefix}: {message}"),
                Author = Sync.ServerId
            };
            MyMultiplayerBase.SendChatMessage(ref msg);
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Message(IMyPlayer player, string message, string prefix = null, params object[] args) => Message(player, string.Format(message, args), prefix);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Reply(IMyPlayer player, string message, string prefix = null) => Message(player, message, prefix);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Reply(IMyPlayer player, string message, string prefix = null, params object[] args) => Reply(player, string.Format(message, args), prefix);

        #endregion Chat and Commands

        #region Item Handling

        // TODO: Implement

        #endregion Item Handling

        #region Inventory Handling

        // TODO: Implement

        #endregion Inventory Handling
    }
}
