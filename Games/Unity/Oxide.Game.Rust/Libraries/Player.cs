using System;
using System.Collections.Generic;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Game.Rust.Libraries
{
    public class Player : Library
    {
        // Covalence references
        internal static readonly RustCovalenceProvider Covalence = RustCore.Covalence;
        internal static readonly IPlayerManager PlayerManager = Covalence.PlayerManager;

        #region Player Administration

        /// <summary>
        /// Bans the player from the server
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        public void Ban(BasePlayer player, string reason = "")
        {
            var iplayer = PlayerManager.FindPlayerById(player.UserIDString);
            iplayer?.Ban(reason);
        }

        /// <summary>
        /// Heals the player by specified amount
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        public void Heal(BasePlayer player, float amount)
        {
            var iplayer = PlayerManager.FindPlayerById(player.UserIDString);
            iplayer?.Heal(amount);
        }

        /// <summary>
        /// Damages the player by specified amount
        /// </summary>
        /// <param name="player"></param>
        /// <param name="amount"></param>
        public void Hurt(BasePlayer player, float amount)
        {
            var iplayer = PlayerManager.FindPlayerById(player.UserIDString);
            iplayer?.Hurt(amount);
        }

        /// <summary>
        /// Kicks the player from the server
        /// </summary>
        /// <param name="player"></param>
        /// <param name="reason"></param>
        public void Kick(BasePlayer player, string reason = "")
        {
            var iplayer = PlayerManager.FindPlayerById(player.UserIDString);
            iplayer?.Kick(reason);
        }

        /// <summary>
        /// Causes the player to die
        /// </summary>
        /// <param name="player"></param>
        public void Kill(BasePlayer player)
        {
            var iplayer = PlayerManager.FindPlayerById(player.UserIDString);
            iplayer?.Kill();
        }

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="player"></param>
        /// <param name="destination"></param>
        public void Teleport(BasePlayer player, Vector3 destination) => Teleport(player, destination.x, destination.y, destination.z);

        /// <summary>
        /// Teleports the player to the target player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="target"></param>
        public void Teleport(BasePlayer player, BasePlayer target)
        {
            var targetPos = Position(target);
            Teleport(player, targetPos.x, targetPos.y, targetPos.z);
        }

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="player"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(BasePlayer player, float x, float y, float z)
        {
            var iplayer = PlayerManager.FindPlayerById(player.UserIDString);
            iplayer?.Teleport(x, y, z);
        }

        #endregion

        #region Player Information

        /// <summary>
        /// Returns the position of player as Vector3
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public Vector3 Position(BasePlayer player) => player.transform.position;

        #endregion

        #region Player Finding

        /// <summary>
        /// Gets the player player using a name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public BasePlayer Session(string nameOrIdOrIp)
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
        /// Gets the player player using a Steam ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BasePlayer SessionById(string id)
        {
            foreach (var player in Players)
            {
                if (!id.Equals(player.UserIDString)) continue;
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
        public void Command(BasePlayer player, string command, params object[] args)
        {
            var iplayer = PlayerManager.FindPlayerById(player.UserIDString);
            iplayer?.Command(command, args);
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Message(BasePlayer player, string message, string prefix = null)
        {
            var iplayer = PlayerManager.FindPlayerById(player.UserIDString);
            iplayer?.Message(prefix != null ? $"{prefix} {message}" : message);
        }

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
        public void Reply(BasePlayer player, string message, string prefix = null) => Message(player, prefix != null ? $"{prefix} {message}" : message);

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
