using System;

using SDG.Unturned;
using Steamworks;
using UnityEngine;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Unturned.Libraries.Covalence
{
    /// <summary>
    /// Represents a connected player
    /// </summary>
    public class UnturnedLivePlayer : ILivePlayer, IPlayerCharacter
    {
        #region Information

        private readonly ulong steamId;

        /// <summary>
        /// Gets the base player of the player
        /// </summary>
        public IPlayer BasePlayer => UnturnedCovalenceProvider.Instance.PlayerManager.GetPlayer(steamId.ToString());

        /// <summary>
        /// Gets the user's in-game character, if available
        /// </summary>
        public IPlayerCharacter Character { get; private set; }

        /// <summary>
        /// Gets the owner of the character
        /// </summary>
        public ILivePlayer Owner => this;

        /// <summary>
        /// Gets the object that backs the character, if available
        /// </summary>
        public object Object { get; private set; }

        /// <summary>
        /// Gets the user's last command type
        /// </summary>
        public CommandType LastCommand { get; set; }

        /// <summary>
        /// Gets the user's IP address
        /// </summary>
        public string Address
        {
            get
            {
                P2PSessionState_t sessionState;
                SteamGameServerNetworking.GetP2PSessionState(steamPlayer.playerID.steamID, out sessionState);
                return Parser.getIPFromUInt32(sessionState.m_nRemoteIP);
            }
        }

        /// <summary>
        /// Gets the user's average network ping
        /// </summary>
        public int Ping => Convert.ToInt32(steamPlayer.ping);

        private readonly SteamPlayer steamPlayer;

        internal UnturnedLivePlayer(SteamPlayer steamPlayer)
        {
            this.steamPlayer = steamPlayer;
            steamId = steamPlayer.playerID.steamID.m_SteamID;
            Character = this;
            Object = steamPlayer.player.transform.gameObject;
        }

        #endregion

        #region Administration

        /// <summary>
        /// Returns if the user is admin
        /// </summary>
        public bool IsAdmin => steamPlayer.isAdmin;

        /// <summary>
        /// Damages player by specified amount
        /// </summary>
        /// <param name="amount"></param>
        public void Hurt(float amount)
        {
            EPlayerKill ePlayerKill;
            steamPlayer.player.life.askDamage((byte)amount, Vector3.up * amount, EDeathCause.KILL, ELimb.SKULL, CSteamID.Nil, out ePlayerKill);
        }

        /// <summary>
        /// Kicks the user from the game
        /// </summary>
        /// <param name="reason"></param>
        public void Kick(string reason) => Provider.kick(steamPlayer.playerID.steamID, reason);

        /// <summary>
        /// Causes the user's character to die
        /// </summary>
        public void Kill()
        {
            EPlayerKill ePlayerKill;
            steamPlayer.player.life.askDamage(101, Vector3.up * 101f, EDeathCause.KILL, ELimb.SKULL, CSteamID.Nil, out ePlayerKill);
        }

        /// <summary>
        /// Teleports the user's character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Teleport(float x, float y, float z)
        {
            var vector31 = steamPlayer.player.transform.rotation.eulerAngles;
            steamPlayer.player.sendTeleport(new Vector3(x, y, z), MeasurementTool.angleToByte(vector31.y));
        }

        #endregion

        #region Chat and Commands

        /// <summary>
        /// Sends the specified message to the user
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Message(string message, params object[] args)
        {
            ChatManager.say(steamPlayer.playerID.steamID, string.Format(message, args), Color.white, EChatMode.LOCAL);
        }

        /// <summary>
        /// Replies to the user with the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public void Reply(string message, params object[] args) => Message(string.Format(message, args));

        /// <summary>
        /// Runs the specified console command on the user
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args)
        {
            Commander.execute(steamPlayer.playerID.steamID, $"{command} {string.Join(" ", Array.ConvertAll(args, x => x.ToString()))}");
        }

        #endregion

        #region Location

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public void Position(out float x, out float y, out float z)
        {
            var pos = steamPlayer.player.transform.position;
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

        /// <summary>
        /// Gets the position of the character
        /// </summary>
        /// <returns></returns>
        public GenericPosition Position()
        {
            var pos = steamPlayer.player.transform.position;
            return new GenericPosition(pos.x, pos.y, pos.z);
        }

        #endregion
    }
}
