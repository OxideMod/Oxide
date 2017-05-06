using Oxide.Core;
using Oxide.Core.Libraries;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game.ModAPI;
using VRage.Network;
using VRageMath;


namespace Oxide.Game.SpaceEngineers.Libraries
{
    public class Player : Library
    {
        // Game references
        #region Information
        private MyTypeTable m_typeTable;
        private static readonly DBNull e = DBNull.Value;

        /// <summary>
        /// Gets the user's language
        /// </summary>
        //public CultureInfo Language(PlayerSession session) => CultureInfo.GetCultureInfo("en"); // TODO: Implement when possible

        /// <summary>
        /// Gets the player's IP address
        /// </summary>
        //public string Address(PlayerSession session) => session.Player.ipAddress;

        /// <summary>
        /// Gets the player's average network ping
        /// </summary>
        //public int Ping(PlayerSession session) => session.Player.averagePing;

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        //public bool IsAdmin(string id) => GameManager.IsAdmin(new CSteamID(Convert.ToUInt64(id)));

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        //public bool IsAdmin(ulong id) => GameManager.IsAdmin(new CSteamID(id));

        /// <summary>
        /// Returns if the player is admin
        /// </summary>
        //public bool IsAdmin(PlayerSession session) => session.IsAdmin;

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        //public bool IsBanned(string id) => BanManager.IsBanned(Convert.ToUInt64(id));

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        //public bool IsBanned(ulong id) => BanManager.IsBanned(id);

        /// <summary>
        /// Gets if the player is banned
        /// </summary>
        //public bool IsBanned(PlayerSession session) => IsBanned(session.SteamId.m_SteamID);

        /// <summary>
        /// Gets if the player is connected
        /// </summary>
        //public bool IsConnected(PlayerSession session) => session.Player.isConnected;

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        //public bool IsSleeping(string id) => session.Identity.Sleeper != null; // TODO: Session is null OnUserDisconnected?

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        //public bool IsSleeping(ulong id) => session.Identity.Sleeper != null; // TODO: Session is null OnUserDisconnected?

        /// <summary>
        /// Returns if the player is sleeping
        /// </summary>
        //public bool IsSleeping(PlayerSession session) => session.Identity.Sleeper != null; // TODO: Session is null OnUserDisconnected?

        #endregion

        #region Administration

        /// <summary>
        /// Bans the player from the server
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        //public void Ban(PlayerSession session, string reason = "")
        //{
        //    // Check if already banned
        //    if (IsBanned(session)) return;

        //    // Ban and kick user
        //    BanManager.AddBan(session.SteamId.m_SteamID);
        //    if (session.Player.isConnected) Kick(session, reason);
        //}

        /// <summary>
        /// Makes the player do an emote
        /// </summary>
        /// <param name="session"></param>
        /// <param name="emote"></param>
        //public void Emote(PlayerSession session, EEmoteType emote)
        //{
        //    var emoteManager = session.WorldPlayerEntity.GetComponent<EmoteManagerServer>();
        //    emoteManager?.BeginEmoteServer(emote);
        //}

        /// <summary>
        /// Heals the player by specified amount
        /// </summary>
        /// <param name="session"></param>
        /// <param name="amount"></param>
        //public void Heal(PlayerSession session, float amount)
        //{
        //    var effect = new EntityEffectFluid(EEntityFluidEffectType.Health, EEntityEffectFluidModifierType.AddValuePure, amount);
        //    var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
        //    effect.Apply(stats);
        //}

        /// <summary>
        /// Damages the player by specified amount
        /// </summary>
        /// <param name="session"></param>
        /// <param name="amount"></param>
        //public void Hurt(PlayerSession session, float amount)
        //{
        //    var effect = new EntityEffectFluid(EEntityFluidEffectType.Damage, EEntityEffectFluidModifierType.AddValuePure, -amount);
        //    var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
        //    effect.Apply(stats);
        //}

        /// <summary>
        /// Kicks the player from the server
        /// </summary>
        /// <param name="session"></param>
        /// <param name="reason"></param>
        //public void Kick(PlayerSession session, string reason = "") => GameManager.KickPlayer(session, reason);

        /// <summary>
        /// Causes the player to die
        /// </summary>
        /// <param name="session"></param>
        //public void Kill(PlayerSession session)
        //{
        //    var stats = session.WorldPlayerEntity.GetComponent<EntityStats>();
        //    var entityEffectSourceDatum = new EntityEffectSourceData { SourceDescriptionKey = "EntityStats/Sources/Suicide" };
        //    stats.HandleEvent(new EntityEventData { EventType = EEntityEventType.Die }, entityEffectSourceDatum);
        //}

        /// <summary>
        /// Renames the player to specified name
        /// <param name="session"></param>
        /// <param name="name"></param>
        /// </summary>
        //public void Rename(PlayerSession session, string name)
        //{
        //    //name = name.Substring(0, 32);
        //    name = ChatManagerServer.CleanupGeneral(name);
        //    if (string.IsNullOrEmpty(name.Trim())) name = "Unnamed";

        //    // Chat/display name
        //    session.Name = name;
        //    session.Identity.Name = name;
        //    session.WorldPlayerEntity.GetComponent<HurtMonoBehavior>().RPC("UpdateName", uLink.RPCMode.All, name);
        //    SteamGameServer.BUpdateUserData(session.SteamId, name, 0);

        //    // Overhead name // TODO: Implement when possible
        //    //var displayProxyName = session.WorldPlayerEntity.GetComponent<DisplayProxyName>();
        //    //displayProxyName.UpdateName(name);
        //}

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="session"></param>
        /// <param name="destination"></param>
        //public void Teleport(PlayerSession session, Vector3 destination) => session.WorldPlayerEntity.transform.position = destination;

        /// <summary>
        /// Teleports the player to the target player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="target"></param>
        //public void Teleport(PlayerSession session, PlayerSession target) => Teleport(session, Position(target));

        /// <summary>
        /// Teleports the player to the specified position
        /// </summary>
        /// <param name="session"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        //public void Teleport(PlayerSession session, float x, float y, float z) => Teleport(session, new Vector3(x, y, z));

        /// <summary>
        /// Unbans the player
        /// </summary>
        //public void Unban(PlayerSession session)
        //{
        //    // Check if unbanned already
        //    if (!IsBanned(session)) return;

        //    // Set to unbanned
        //    BanManager.RemoveBan(session.SteamId.m_SteamID);
        //}

        #endregion

        #region Location

        /// <summary>
        /// Returns the position of player as Vector3
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public Vector3 Position(IMyPlayer session) => session.GetPosition();

        #endregion

        #region Player Finding

        /// <summary>
        /// Gets the player session using a name, Steam ID, or IP address
        /// </summary>
        /// <param name="nameOrIdOrIp"></param>
        /// <returns></returns>
        public IMyPlayer Find(string nameOrIdOrIp)
        {
            IMyPlayer session = null;
            foreach (var s in Sessions)
            {
                if (!nameOrIdOrIp.Equals(s.DisplayName, StringComparison.OrdinalIgnoreCase) &&
                    !nameOrIdOrIp.Equals(s.SteamUserId.ToString())) continue;
                session = s;
                break;
            }
            return session;
        }

        /// <summary>
        /// Gets the player session using a uLink.NetworkPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        //public PlayerSession Find(uLink.NetworkPlayer player) => GameManager.Instance.GetSession(player);

        /// <summary>
        /// Gets the player session using a UnityEngine.Collider
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        //public PlayerSession Find(Collider col)
        //{
        //    PlayerSession session = null;
        //    var stats = col.gameObject.GetComponent<EntityStatsTriggerProxy>().Stats;
        //    foreach (var s in Sessions)
        //    {
        //        if (!s.Value.WorldPlayerEntity.GetComponent<EntityStats>() == stats) continue;
        //        session = s.Value;
        //        break;
        //    }
        //    return session;
        //}

        /// <summary>
        /// Gets the player session using a UnityEngine.GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        //public PlayerSession Find(GameObject go)
        //{
        //    var sessions = GameManager.Instance.GetSessions();
        //    return (from i in sessions where go.Equals(i.Value.WorldPlayerEntity) select i.Value).FirstOrDefault();
        //}

        /// <summary>
        /// Gets the player session using a Steam ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //public PlayerSession FindById(string id)
        //{
        //    PlayerSession session = null;
        //    foreach (var s in Sessions)
        //    {
        //        if (!id.Equals(s.Value.SteamId.ToString())) continue;
        //        session = s.Value;
        //        break;
        //    }
        //    return session;
        //}

        /// <summary>
        /// Gets the player session using a uLink.NetworkPlayer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public IMyPlayer Find(ulong steamId)
        {
            List<IMyPlayer> m_players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(m_players, x => x.SteamUserId == steamId);

            return m_players.Count == 0 ? null : m_players[0];
        }

        /// <summary>
        /// Gets the player session using a UnityEngine.GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        //public PlayerSession Session(GameObject go)
        //{
        //    return (from s in Sessions where go.Equals(s.Value.WorldPlayerEntity) select s.Value).FirstOrDefault();
        //}

        /// <summary>
        /// Returns all connected sessions
        /// </summary>
        public List<IMyPlayer> Sessions
        {
            get
            {
                List<IMyPlayer> m_players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(m_players);
                return m_players;
            }
        }
        //public Dictionary<uLink.NetworkPlayer, PlayerSession> Sessions => GameManager.GetSessions();

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Runs the specified player command
        /// </summary>
        /// <param name="session"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(IMyPlayer session, string command, params object[] args)
        {
            // TODO: Implement when possible
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Message(IMyPlayer session, string message, string prefix = null)
        {
            if (string.IsNullOrEmpty(message) && string.IsNullOrEmpty(prefix)) return;
            ScriptedChatMsg msg = new ScriptedChatMsg();
            msg.Text = string.IsNullOrEmpty(prefix) ? message : (string.IsNullOrEmpty(message) ? prefix : $"{prefix}: {message}");
            msg.Author = "server";
            msg.Target = session.IdentityId;
            msg.Font = "Green";
            MyMultiplayerBase.SendScriptedChatMessage(ref msg);
            
            //var messageMethod = typeof(MyMultiplayerBase).GetMethod("OnScriptedChatMessageRecieved", BindingFlags.NonPublic | BindingFlags.Static);
            //RaiseStaticEvent(messageMethod, new EndpointId(session.SteamUserId), msg);
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Message(IMyPlayer session, string message, string prefix = null, params object[] args) => Message(session, string.Format(message, args), prefix);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Reply(IMyPlayer session, string message, string prefix = null) => Message(session, message, prefix);

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Reply(IMyPlayer session, string message, string prefix = null, params object[] args) => Reply(session, string.Format(message, args), prefix);


        /// <summary>
        /// inject network message to the space engineer network layer direct
        /// </summary>
        /// <param name="method"></param>
        /// <param name="endpoint"></param>
        /// <param name="args"></param>
        internal void RaiseStaticEvent(MethodInfo method, EndpointId endpoint, params object[] args)
        {
            Interface.Oxide.ServerConsole.AddMessage($"RaiseStaticEvent {method} {endpoint} {args}");

            //array to hold arguments to pass into DispatchEvent
            object[] arguments = new object[11];
            Interface.Oxide.ServerConsole.AddMessage("1");
            arguments[0] = TryGetStaticCallSite(method);
            arguments[1] = endpoint;
            arguments[2] = 1f;
            arguments[3] = (IMyEventOwner)null;
            Interface.Oxide.ServerConsole.AddMessage("2");

            //copy supplied arguments into the reflection arguments
            for (int i = 0; i < args.Length; i++)
                arguments[i + 4] = args[i];
            Interface.Oxide.ServerConsole.AddMessage("4");

            //pad the array out with DBNull
            for (int j = args.Length + 4; j < 10; j++)
                arguments[j] = e;
            Interface.Oxide.ServerConsole.AddMessage("5");

            arguments[10] = (IMyEventOwner)null;

            Interface.Oxide.LogDebug($" 6");
            //create an array of Types so we can create a generic method
            Type[] argTypes = new Type[8];

            for (int k = 3; k < 11; k++)
                argTypes[k - 3] = arguments[k]?.GetType() ?? typeof(IMyEventOwner);
            Interface.Oxide.LogDebug($" 7");

            //create a generic method of DispatchEvent and invoke to inject our data into the network
            var dispatch = typeof(MyReplicationLayerBase).GetMethod("DispatchEvent", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(argTypes);
            Interface.Oxide.LogDebug($" * DispatchEvent = {dispatch}");
            dispatch.Invoke(MyMultiplayer.ReplicationLayer, arguments);
        }

        private CallSite TryGetStaticCallSite(MethodInfo method)
        {
            if (m_typeTable == null)
                m_typeTable = typeof(MyReplicationLayerBase).GetField("m_typeTable", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(MyMultiplayer.ReplicationLayer) as MyTypeTable;

            Interface.Oxide.LogDebug($" TryGetStaticCallSite = {method} {m_typeTable}");
            var methodLookup = (Dictionary<MethodInfo, CallSite>)typeof(MyEventTable).GetField("m_methodInfoLookup", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(m_typeTable.StaticEventTable);
            Interface.Oxide.LogDebug($" methodLookup = {methodLookup}");
            CallSite result;
            if (!methodLookup.TryGetValue(method, out result))
                throw new MissingMemberException("Provided event target not found!");
            return result;
        }

        #endregion

        #region Item Handling

        /// <summary>
        /// Drops item by item ID from player's inventory
        /// </summary>
        /// <param name="session"></param>
        /// <param name="itemId"></param>
        //public void DropItem(PlayerSession session, int itemId)
        //{
        //    var position = session.WorldPlayerEntity.transform.position;
        //    var inventory = Inventory(session);
        //    for (var s = 0; s < inventory.Capacity; s++)
        //    {
        //        var i = inventory.GetSlot(s);
        //        if (i.Item.ItemId == itemId) inventory.DropSlot(s, (position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
        //    }
        //}

        /// <summary>
        /// Drops item from the player's inventory
        /// </summary>
        /// <param name="session"></param>
        /// <param name="item"></param>
        //public void DropItem(PlayerSession session, IItem item)
        //{
        //    var position = session.WorldPlayerEntity.transform.position;
        //    var inventory = Inventory(session);
        //    for (var s = 0; s < inventory.Capacity; s++)
        //    {
        //        var i = inventory.GetSlot(s);
        //        if (i.Item == item) inventory.DropSlot(s, (position + new Vector3(0f, 1f, 0f)) + (position / 2f), (position + new Vector3(0f, 0.2f, 0f)) * 8f);
        //    }
        //}

        /// <summary>
        /// Gives quantity of an item to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="itemId"></param>
        /// <param name="quantity"></param>
        //public void GiveItem(PlayerSession session, int itemId, int quantity = 1) => GiveItem(session, Item.GetItem(itemId), quantity);

        /// <summary>
        /// Gives quantity of an item to the player
        /// </summary>
        /// <param name="session"></param>
        /// <param name="item"></param>
        /// <param name="quantity"></param>
        //public void GiveItem(PlayerSession session, IItem item, int quantity = 1) => ItemManager.GiveItem(session.Player, item, quantity);

        #endregion

        #region Inventory Handling

        /// <summary>
        /// Gets the inventory of the player
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        //public PlayerInventory Inventory(PlayerSession session) => session.WorldPlayerEntity.GetComponent<PlayerInventory>();

        /// <summary>
        /// Clears the inventory of the player
        /// </summary>
        /// <param name="session"></param>
        //public void ClearInventory(PlayerSession session) => Inventory(session)?.DestroyAll();

        #endregion
    }
}
