﻿using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core;
using Emotes;
using Oxide.Core.Libraries;
using Steamworks;
using UnityEngine;

namespace Oxide.Game.Hurtworld.Libraries
{
    public class Player : Library
    {
        // Game references
        internal static readonly BanManager BanManager = BanManager.Instance;
        internal static readonly GameManager GameManager = GameManager.Instance;
        internal static readonly GlobalItemManager ItemManager = GlobalItemManager.Instance;

        #region Information

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address(PlayerSession session) => session.Player.ipAddress;

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping(PlayerSession session) => session.Player.averagePing;

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin(PlayerSession session) => session.IsAdmin;

        /// <summary>
        /// Gets if the user is banned
        /// </summary>
        public bool IsBanned(PlayerSession session) => BanManager.Instance.IsBanned(session.SteamId.m_SteamID);

        /// <summary>
        /// Gets if the user is connected
        /// </summary>
        public bool IsConnected(PlayerSession session) => session.IsLoaded;

        /// <summary>
        /// Returns if the user is sleeping
        /// </summary>
        public bool IsSleeping(PlayerSession session) => session.Identity.Sleeper != null;

        #endregion

        #region Administration

        /// <summary>
        /// Bans the player from the server
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        public void Ban(PlayerSession session, string reason = "")
        {
            // Check if already banned
            if (IsBanned(session)) return;

            // Ban and kick user
            BanManager.AddBan(session.SteamId.m_SteamID);
            if (session.IsLoaded) Kick(session, reason);
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
            var effect = new EntityEffectFluid(EEntityFluidEffectType.Health, EEntityEffectFluidModifierType.AddValuePure, amount);
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            effect.Apply(stats);
        }

        /// <summary>
        /// Damages the player by specified amount
        /// </summary>
        /// <param name="session"></param>
        /// <param name="amount"></param>
        public void Hurt(PlayerSession session, float amount)
        {
            var effect = new EntityEffectFluid(EEntityFluidEffectType.Damage, EEntityEffectFluidModifierType.AddValuePure, -amount);
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            effect.Apply(stats);
        }

        /// <summary>
        /// Kicks the player from the server
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        public void Kick(PlayerSession session, string reason = "") => GameManager.KickPlayer(session, reason);

        /// <summary>
        /// Causes the player to die
        /// </summary>
        /// <param name="session"></param>
        public void Kill(PlayerSession session)
        {
            var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            var entityEffectSourceDatum = new EntityEffectSourceData { SourceDescriptionKey = "EntityStats/Sources/Suicide" };
            stats.HandleEvent(new EntityEventData { EventType = EEntityEventType.Die }, entityEffectSourceDatum);
        }

        /// <summary>
        /// Renames the player to specified name
        /// <param name="session"></param>
        /// <param name="name"></param>
        /// </summary>
        public void Rename(PlayerSession session, string name)
        {
            //name = name.Substring(0, 32);
            name = ChatManagerServer.CleanupGeneral(name);
            if (string.IsNullOrEmpty(name.Trim())) name = "Unnamed";

            // Chat/display name
            session.Name = name;
            session.Identity.Name = name;
            session.WorldPlayerEntity.GetComponent<HurtMonoBehavior>().RPC("UpdateName", uLink.RPCMode.All, name);
            SteamGameServer.BUpdateUserData(session.SteamId, name, 0);

            // Overhead name // TODO: Implement when possible
            //var displayProxyName = session.WorldPlayerEntity.GetComponent<DisplayProxyName>();
            //displayProxyName.UpdateName(name);
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
        public void Teleport(PlayerSession session, float x, float y, float z) => session.WorldPlayerEntity.transform.position = new Vector3(x, y, z);

        /// <summary>
        /// Unbans the player
        /// </summary>
        public void Unban(PlayerSession session)
        {
            // Check not banned
            if (!IsBanned(session)) return;

            // Set to unbanned
            ConsoleManager.Instance.ExecuteCommand($"unban {session.SteamId}");
        }

        #endregion

        #region Location

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
            PlayerSession session = null;
            foreach (var s in Sessions)
            {
                if (!nameOrIdOrIp.Equals(s.Value.Name, StringComparison.OrdinalIgnoreCase) &&
                    !nameOrIdOrIp.Equals(s.Value.SteamId.ToString()) && !nameOrIdOrIp.Equals(s.Key.ipAddress)) continue;
                session = s.Value;
                break;
            }
            return session;
        }

        /// <summary>
        /// Gets the player session using a Steam ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PlayerSession SessionById(string id)
        {
            PlayerSession session = null;
            foreach (var s in Sessions)
            {
                if (!id.Equals(s.Value.SteamId.ToString())) continue;
                session = s.Value;
                break;
            }
            return session;
        }

        /// <summary>
        /// Gets the player session using a Collider
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public PlayerSession Session(Collider col)
        {
            PlayerSession session = null;
            var stats = col.gameObject.GetComponent<EntityStatsTriggerProxy>().Stats;
            foreach (var s in Sessions)
            {
                if (!s.Value.WorldPlayerEntity.GetComponent<EntityStats>() == stats) continue;
                session = s.Value;
                break;
            }
            return session;
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
            // TODO: Implement when possible
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Message(PlayerSession session, string message, string prefix = null)
        {
            ChatManagerServer.Instance.RPC("RelayChat", session.Player, prefix != null ? $"{prefix}: {message}" : message);
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
