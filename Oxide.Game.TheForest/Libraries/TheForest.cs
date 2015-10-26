using System.Linq;

using Bolt;
using Steamworks;
using TheForest.Utils;

using Oxide.Core.Libraries;

namespace Oxide.Game.TheForest.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for The Forest
    /// </summary>
    public class TheForest : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        [LibraryFunction("PlayerFromId")]
        public BoltEntity PlayerFromId(NetworkId id) => Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.networkId == id);

        [LibraryFunction("PlayerFromEvent")]
        public BoltEntity PlayerFromEvent(ChatEvent e) => Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.networkId == e.Sender);

        [LibraryFunction("PlayerFromConnection")]
        public BoltEntity PlayerFromConnection(BoltConnection con) => Scene.SceneTracker.allPlayerEntities.FirstOrDefault(ent => ent.source.RemoteEndPoint.SteamId.Id == con.RemoteEndPoint.SteamId.Id);

        [LibraryFunction("IdFromPlayer")]
        public ulong IdFromPlayer(BoltEntity player) => player.source.RemoteEndPoint.SteamId.Id;

        [LibraryFunction("IdFromConnection")]
        public ulong IdFromConnection(BoltConnection con) => con.RemoteEndPoint.SteamId.Id;

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
