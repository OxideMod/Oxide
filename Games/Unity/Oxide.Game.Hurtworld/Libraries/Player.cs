using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core;
using Emotes;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Hurtworld.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Game.Hurtworld.Libraries
{
    public class Player : Library
    {
        // Covalence references
        internal static readonly HurtworldCovalenceProvider Covalence = HurtworldCore.Covalence;
        internal static readonly IPlayerManager PlayerManager = Covalence.PlayerManager;

        // Game references
        internal static readonly GlobalItemManager ItemManager = GlobalItemManager.Instance;

        #region Player Administration

        /// <summary>
        /// Bans the player from the server
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        public void Ban(PlayerSession session, string reason = "")
        {
            var iplayer = PlayerManager.FindPlayerById(session.SteamId.ToString());
            iplayer?.Ban(reason);
        }

        /// <summary>
        /// Makes the player do an emote
        /// </summary>
        /// <param name="session"></param>
        /// <param name="emote"></param>
        public void Emote(PlayerSession session, EEmoteType emote)
        {
            var emoteManager = session.WorldPlayerEntity.GetComponent<EmoteManagerServer>();
            emoteManager?.BeginEmoteServer(emote);
        }

        /// <summary>
        /// Heals the player by specified amount
        /// </summary>
        /// <param name="session"></param>
        /// <param name="amount"></param>
        public void Heal(PlayerSession session, float amount)
        {
            var iplayer = PlayerManager.FindPlayerById(session.SteamId.ToString());
            iplayer?.Heal(amount);
        }

        /// <summary>
        /// Damages the player by specified amount
        /// </summary>
        /// <param name="session"></param>
        /// <param name="amount"></param>
        public void Hurt(PlayerSession session, float amount)
        {
            var iplayer = PlayerManager.FindPlayerById(session.SteamId.ToString());
            iplayer?.Hurt(amount);
        }

        /// <summary>
        /// Kicks the player from the server
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        public void Kick(PlayerSession session, string reason = "")
        {
            var iplayer = PlayerManager.FindPlayerById(session.SteamId.ToString());
            iplayer?.Kick(reason);
        }

        /// <summary>
        /// Causes the player to die
        /// </summary>
        /// <param name="session"></param>
        public void Kill(PlayerSession session)
        {
            var iplayer = PlayerManager.FindPlayerById(session.SteamId.ToString());
            iplayer?.Kill();
        }

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="session"></param>
        /// <param name="destination"></param>
        public void Teleport(PlayerSession session, Vector3 destination) => Teleport(session, destination.x, destination.y, destination.z);

        /// <summary>
        /// Teleports the player to the target player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="target"></param>
        public void Teleport(PlayerSession session, PlayerSession target)
        {
            var targetPos = Position(target);
            Teleport(session, targetPos.x, targetPos.y, targetPos.z);
        }

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="session"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(PlayerSession session, float x, float y, float z)
        {
            var iplayer = PlayerManager.FindPlayerById(session.SteamId.ToString());
            iplayer?.Teleport(x, y, z);
        }

        #endregion

        #region Player Information

        /// <summary>
        /// Returns the position of player as Vector3
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Vector3 Position(PlayerSession session) => session.WorldPlayerEntity.transform.position;

        #endregion

        #region Player Finding

        /// <summary>
        /// Gets the player session using a name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public PlayerSession Session(string nameOrIdOrIp)
        {
            foreach (var session in Sessions)
            {
                if (!nameOrIdOrIp.Equals(session.Value.Name, StringComparison.OrdinalIgnoreCase) &&
                    !nameOrIdOrIp.Equals(session.Value.SteamId.ToString()) && !nameOrIdOrIp.Equals(session.Key.ipAddress)) continue;
                return session.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets the player session using a Steam ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PlayerSession SessionById(string id)
        {
            foreach (var session in Sessions)
            {
                if (!id.Equals(session.Value.SteamId.ToString())) continue;
                return session.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets the player session using a Collider
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public PlayerSession Session(Collider col)
        {
            var stats = col.gameObject.GetComponent<EntityStatsTriggerProxy>().Stats;
            foreach (var session in Sessions)
            {
                if (!session.Value.WorldPlayerEntity.GetComponent<EntityStats>() == stats) continue;
                return session.Value;
            }
            return null;
        }

        /// <summary>
        /// Gets the player session using a uLink.NetworkPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public PlayerSession Session(uLink.NetworkPlayer player) => GameManager.Instance.GetSession(player);

        /// <summary>
        /// Gets the player session using a UnityEngine.GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public PlayerSession Session(GameObject go)
        {
            return (from s in Sessions where go.Equals(s.Value.WorldPlayerEntity) select s.Value).FirstOrDefault();
        }

        /// <summary>
        /// Returns all connected sessions
        /// </summary>
        public Dictionary<uLink.NetworkPlayer, PlayerSession> Sessions => GameManager.Instance.GetSessions();

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Runs the specified player command
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(PlayerSession session, string command, params object[] args)
        {
            var iplayer = PlayerManager.FindPlayerById(session.SteamId.ToString());
            iplayer?.Command(command, args);
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Message(PlayerSession session, string message, string prefix = null)
        {
            var iplayer = PlayerManager.FindPlayerById(session.SteamId.ToString());
            iplayer?.Message(prefix != null ? $"{prefix} {message}" : message);
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Message(PlayerSession session, string message, string prefix = null, params object[] args) => Message(session, string.Format(message, args), prefix);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Reply(PlayerSession session, string message, string prefix = null) => Message(session, prefix != null ? $"{prefix} {message}" : message);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Reply(PlayerSession session, string message, string prefix = null, params object[] args) => Reply(session, string.Format(message, args), prefix);

        #endregion

        #region Item Handling

        /// <summary>
        /// Drops item by item ID from player's inventory
        /// </summary>
        /// <param name="session"></param>
        /// <param name="itemId"></param>
        public void DropItem(PlayerSession session, int itemId)
        {
            var position = session.WorldPlayerEntity.transform.position;
            var inventory = Inventory(session);
            for (var s = 0; s < inventory.Capacity; s++)
            {
                var i = inventory.GetSlot(s);
                if (i.Item.ItemId == itemId) inventory.DropSlot(s, (position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
            }
        }

        /// <summary>
        /// Drops item from the player's inventory
        /// </summary>
        /// <param name="session"></param>
        /// <param name="item"></param>
        public void DropItem(PlayerSession session, IItem item)
        {
            var position = session.WorldPlayerEntity.transform.position;
            var inventory = Inventory(session);
            for (var s = 0; s < inventory.Capacity; s++)
            {
                var i = inventory.GetSlot(s);
                if (i.Item == item) inventory.DropSlot(s, (position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
            }
        }

        /// <summary>
        /// Gives quantity of an item to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        public void GiveItem(PlayerSession session, int itemId, int quantity = 1) => GiveItem(session, Item.GetItem(itemId), quantity);

        /// <summary>
        /// Gives quantity of an item to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="item"></param>
        /// <param name="quantity"></param>
        public void GiveItem(PlayerSession session, IItem item, int quantity = 1) => ItemManager.GiveItem(session.Player, item, quantity);

        #endregion

        #region Inventory Handling

        /// <summary>
        /// Gets the inventory of the player
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public PlayerInventory Inventory(PlayerSession session) => session.WorldPlayerEntity.GetComponent<PlayerInventory>();

        /// <summary>
        /// Clears the inventory of the player
        /// </summary>
        /// <param name="session"></param>
        public void ClearInventory(PlayerSession session) => Inventory(session)?.DestroyAll();

        #endregion
    }
}
