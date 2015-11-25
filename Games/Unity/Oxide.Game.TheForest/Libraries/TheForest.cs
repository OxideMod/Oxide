using System.Linq;
using System.Reflection;

using Bolt;
using Steamworks;
using TheForest.Utils;

using Oxide.Core.Libraries;
using Oxide.Plugins;

namespace Oxide.Game.TheForest.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions
    /// </summary>
    public class TheForest : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        /// <returns></returns>
        public override bool IsGlobal => false;

        /// <summary>
        /// Gets private bindingflag for accessing private methods, fields, and properties
        /// </summary>
        [LibraryFunction("PrivateBindingFlag")]
        public BindingFlags PrivateBindingFlag() => (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="str"></param>
        [LibraryFunction("QuoteSafe")]
        public string QuoteSafe(string str) => str.Quote();

        /// <summary>
        /// Returns the Steam ID for the specified player as a string
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        [LibraryFunction("IdFromPlayer")]
        public ulong IdFromPlayer(BoltEntity player) => player.source.RemoteEndPoint.SteamId.Id;

        /// <summary>
        /// Returns the player for the specified network ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [LibraryFunction("PlayerFromId")]
        public BoltEntity PlayerFromId(NetworkId id) => Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.networkId == id);

        /// <summary>
        /// Returns the Steam ID from the specified connection as a string
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [LibraryFunction("IdFromConnection")]
        public ulong IdFromConnection(BoltConnection connection) => connection.RemoteEndPoint.SteamId.Id;

        /// <summary>
        /// Returns the player from the specified connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        [LibraryFunction("PlayerFromConnection")]
        public BoltEntity PlayerFromConnection(BoltConnection connection)
        {
            return Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.source.RemoteEndPoint.SteamId.Id == connection.RemoteEndPoint.SteamId.Id);
        }

        /// <summary>
        /// Returns the player of a NetworkEvent
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        [LibraryFunction("PlayerFromEvent")]
        public BoltEntity PlayerFromEvent(ChatEvent e) => Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.networkId == e.Sender);

        /// <summary>
        /// Returns the name from the specified Steam ID
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        [LibraryFunction("NameFromId")]
        public string NameFromId(ulong steamId) => SteamFriends.GetFriendPersonaName(new CSteamID(steamId));

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="message"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string message)
        {
            if (!BoltNetwork.isRunning) return;

            var e = ChatEvent.Create(GlobalTargets.AllClients);
            e.Message = message;
            e.Sender = LocalPlayer.Entity.networkId;
            e.Send();
        }

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="id"></param>
        /// <param name="message"></param>
        public void BroadcastChat(NetworkId id, string message)
        {
            if (!BoltNetwork.isRunning) return;

            var e = ChatEvent.Create(GlobalTargets.AllClients);
            e.Message = message;
            e.Sender = id;
            e.Send();
        }

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="message"></param>
        public void BroadcastChat(BoltEntity entity, string message)
        {
            if (!BoltNetwork.isRunning) return;

            var e = ChatEvent.Create(GlobalTargets.AllClients);
            e.Message = message;
            e.Sender = entity.networkId;
            e.Send();
        }

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        public void BroadcastChat(BoltConnection connection, string message)
        {
            if (!BoltNetwork.isRunning) return;

            var player = Scene.SceneTracker.allPlayerEntities.FirstOrDefault(entity => entity.source.RemoteEndPoint.SteamId.Id == connection.RemoteEndPoint.SteamId.Id);
            if (!player) return;

            var e = ChatEvent.Create(GlobalTargets.AllClients);
            e.Message = message;
            e.Sender = player.networkId;
            e.Send();
        }

        //ChatEvent.Create();
        //ChatEvent.Create(entity);
        //ChatEvent.Create(connection);
        //ChatEvent.Create(GlobalTargets.AllClients) // Everyone, Others, OnlyServer, AllClients, OnlySelf
        //ChatEvent.Create(entity, EntityTargets.Everyone); // Everyone, EveryoneExceptOwner, EveryoneExceptController, OnlyController, OnlyOwner, OnlySelf, EveryoneExceptOwnerAndController
    }
}
