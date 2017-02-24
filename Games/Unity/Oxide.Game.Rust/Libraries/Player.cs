using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Game.Rust.Libraries
{
    public class Player : Library
    {
        // Covalence references
        internal static readonly RustCovalenceProvider Covalence = RustCovalenceProvider.Instance;
        internal static readonly IPlayerManager PlayerManager = Covalence.PlayerManager;

        #region Information

        /// <summary>
        /// Gets the player's language
        /// </summary>
        public CultureInfo Language(BasePlayer player) => CultureInfo.GetCultureInfo(player.net.connection.info.GetString("global.language") ?? "en");

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        public string Address(BasePlayer player) => Regex.Replace(player.net.connection.ipaddress, @":{1}[0-9]{1}\d*", ""); // TODO: Move IP regex to utility method and make static

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        public int Ping(BasePlayer player) => Network.Net.sv.GetAveragePing(player.net.connection);

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin(string id) => ServerUsers.Is(Convert.ToUInt64(id), ServerUsers.UserGroup.Owner);

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin(ulong id) => ServerUsers.Is(id, ServerUsers.UserGroup.Owner);

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        public bool IsAdmin(BasePlayer player) => IsBanned(player.userID);

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned(string id) => ServerUsers.Is(Convert.ToUInt64(id), ServerUsers.UserGroup.Banned);

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned(ulong id) => ServerUsers.Is(id, ServerUsers.UserGroup.Banned);

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        public bool IsBanned(BasePlayer player) => IsBanned(player.userID);

        /// <summary>
        /// Gets if the player is connected
        /// </summary>
        public bool IsConnected(BasePlayer player) => BasePlayer.activePlayerList.Contains(player);

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping(string id) => BasePlayer.FindSleeping(Convert.ToUInt64(id));

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping(ulong id) => BasePlayer.FindSleeping(id);

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        public bool IsSleeping(BasePlayer player) => IsSleeping(player.userID);

        #endregion

        #region Administration

        /// <summary>
        /// Bans the player from the server
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        public void Ban(BasePlayer player, string reason = "")
        {
            // Check if already banned
            if (IsBanned(player)) return;

            // Ban and kick user
            ServerUsers.Set(player.userID, ServerUsers.UserGroup.Banned, player.displayName, reason);
            ServerUsers.Save();
            if (IsConnected(player)) Kick(player, reason);
        }

        /// <summary>
        /// Heals the player by specified amount
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        public void Heal(BasePlayer player, float amount) => player.Heal(amount);

        /// <summary>
        /// Damages the player by specified amount
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        public void Hurt(BasePlayer player, float amount) => player.Hurt(amount);

        /// <summary>
        /// Kicks the player from the server
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        public void Kick(BasePlayer player, string reason = "") => player.Kick(reason);

        /// <summary>
        /// Causes the player to die
        /// </summary>
        /// <param name="player"></param>
        public void Kill(BasePlayer player) => player.Die();

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        public void Teleport(BasePlayer player, Vector3 destination)
        {
            if (player.IsSpectating()) return;

            player.MovePosition(destination);
            player.ClientRPCPlayer(null, player, "ForcePositionTo", destination);
        }

        /// <summary>
        /// Teleports the player to the target player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        public void Teleport(BasePlayer player, BasePlayer target) => Teleport(player, Position(target));

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="player"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban(BasePlayer player)
        {
            // Check if unbanned already
            if (!IsBanned(player)) return;

            // Set to unbanned
            ServerUsers.Remove(player.userID);
            ServerUsers.Save();
        }

        #endregion

        #region Location

        /// <summary>
        /// Returns the position of player as Vector3
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Vector3 Position(BasePlayer player) => player.transform.position;

        #endregion

        #region Player Finding

        /// <summary>
        /// Gets the player object using a name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public BasePlayer Find(string nameOrIdOrIp)
        {
            foreach (var player in Players)
            {
                if (!nameOrIdOrIp.Equals(player.displayName, StringComparison.OrdinalIgnoreCase) &&
                    !nameOrIdOrIp.Equals(player.UserIDString) && !nameOrIdOrIp.Equals(player.net.connection.ipaddress)) continue;
                return player;
            }
            return null;
        }

        /// <summary>
        /// Gets the player object using a Steam ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BasePlayer FindById(string id)
        {
            foreach (var player in Players)
            {
                if (!id.Equals(player.UserIDString)) continue;
                return player;
            }
            return null;
        }

        /// <summary>
        /// Gets the player object using a Steam ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BasePlayer FindById(ulong id)
        {
            foreach (var player in Players)
            {
                if (!id.Equals(player.userID)) continue;
                return player;
            }
            return null;
        }

        /// <summary>
        /// Returns all connected players
        /// </summary>
        public List<BasePlayer> Players => BasePlayer.activePlayerList;

        /// <summary>
        /// Returns all sleeping players
        /// </summary>
        public List<BasePlayer> Sleepers => BasePlayer.sleepingPlayerList;

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Runs the specified player command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(BasePlayer player, string command, params object[] args) => player.SendConsoleCommand(command, args);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Message(BasePlayer player, string message, string prefix = null) => player.ChatMessage(prefix != null ? $"{prefix} {message}" : message);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Message(BasePlayer player, string message, string prefix = null, params object[] args) => Message(player, string.Format(message, args), prefix);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Reply(BasePlayer player, string message, string prefix = null) => Message(player, message, prefix);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Reply(BasePlayer player, string message, string prefix = null, params object[] args) => Reply(player, string.Format(message, args), prefix);

        #endregion

        #region Item Handling

        /// <summary>
        /// Drops item by item ID from player's inventory
        /// </summary>
        /// <param name="player"></param>
        /// <param name="itemId"></param>
        public void DropItem(BasePlayer player, int itemId)
        {
            var position = player.transform.position;
            var inventory = Inventory(player);
            for (var s = 0; s < inventory.containerMain.capacity; s++)
            {
                var i = inventory.containerMain.GetSlot(s);
                if (i.info.itemid == itemId) i.Drop((position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
            }
            for (var s = 0; s < inventory.containerBelt.capacity; s++)
            {
                var i = inventory.containerBelt.GetSlot(s);
                if (i.info.itemid == itemId) i.Drop((position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
            }
            for (var s = 0; s < inventory.containerWear.capacity; s++)
            {
                var i = inventory.containerWear.GetSlot(s);
                if (i.info.itemid == itemId) i.Drop((position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
            }
        }

        /// <summary>
        /// Drops item from the player's inventory
        /// </summary>
        /// <param name="player"></param>
        /// <param name="item"></param>
        public void DropItem(BasePlayer player, global::Item item)
        {
            var position = player.transform.position;
            var inventory = Inventory(player);
            for (var s = 0; s < inventory.containerMain.capacity; s++)
            {
                var i = inventory.containerMain.GetSlot(s);
                if (i == item) i.Drop((position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
            }
            for (var s = 0; s < inventory.containerBelt.capacity; s++)
            {
                var i = inventory.containerBelt.GetSlot(s);
                if (i == item) i.Drop((position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
            }
            for (var s = 0; s < inventory.containerWear.capacity; s++)
            {
                var i = inventory.containerWear.GetSlot(s);
                if (i == item) i.Drop((position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
            }
        }

        /// <summary>
        /// Gives quantity of an item to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        public void GiveItem(BasePlayer player, int itemId, int quantity = 1) => GiveItem(player, Item.GetItem(itemId), quantity);

        /// <summary>
        /// Gives quantity of an item to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="item"></param>
        /// <param name="quantity"></param>
        public void GiveItem(BasePlayer player, global::Item item, int quantity = 1) => player.inventory.GiveItem(ItemManager.CreateByItemID(item.info.itemid, quantity));

        #endregion

        #region Inventory Handling

        /// <summary>
        /// Gets the inventory of the player
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PlayerInventory Inventory(BasePlayer player) => player.inventory;

        /// <summary>
        /// Clears the inventory of the player
        /// </summary>
        /// <param name="player"></param>
        public void ClearInventory(BasePlayer player) => Inventory(player)?.DoDestroy();

        #endregion
    }
}
