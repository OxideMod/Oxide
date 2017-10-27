using Oxide.Core;
using Oxide.Core.Libraries;
using System.Linq;
using System.Reflection;

namespace Oxide.Game.RustLegacy.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for Rust Legacy
    /// </summary>
    public class RustLegacy : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        /// <returns></returns>
        public override bool IsGlobal => false;

        #region Utility

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

        #endregion Utility

        #region Chat

        /// <summary>
        /// Print a message to every players chat log
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null)
        {
            ConsoleNetworker.Broadcast(message != null ? $"chat.add {name.Quote()} {message.Quote()}" : $"chat.add \"Server\" {name.Quote()}");
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(NetUser netUser, string name, string message = null)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer,
                message != null ? $"chat.add {name.Quote()} {message.Quote()}" : $"chat.add \"Server\" {name.Quote()}");
        }

        #endregion Chat

        #region Console

        /// <summary>
        /// Print a message to the players console log
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [LibraryFunction("SendConsoleMessage")]
        public void SendConsoleMessage(NetUser netUser, string format, params object[] args)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, $"echo {string.Format(format, args)}");
        }

        /// <summary>
        /// Print a message to every players console log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [LibraryFunction("BroadcastConsole")]
        public void BroadcastConsole(string format, params object[] args) => ConsoleNetworker.Broadcast($"echo {string.Format(format, args)}");

        #endregion Console

        #region Commands

        /// <summary>
        /// Runs a console command for a specific player
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="cmd"></param>
        [LibraryFunction("RunClientCommand")]
        public void RunClientCommand(NetUser netUser, string cmd) => ConsoleNetworker.SendClientCommand(netUser.networkPlayer, cmd);

        /// <summary>
        /// Runs a console command on the server
        /// </summary>
        /// <param name="cmd"></param>
        [LibraryFunction("RunServerCommand")]
        public void RunServerCommand(string cmd) => ConsoleSystem.Run(cmd);

        #endregion Commands

        /// <summary>
        /// Finds the player by name, steam id or ip
        /// </summary>
        /// <param name="strNameOrIdorIp"></param>
        [LibraryFunction("FindPlayer")]
        public NetUser FindPlayer(string strNameOrIdorIp)
        {
            NetUser netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.userID.ToString() == strNameOrIdorIp)?.netUser) != null)
                return netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.displayName.ToLower().Contains(strNameOrIdorIp.ToLower()))?.netUser) != null)
                return netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.networkPlayer.ipAddress == strNameOrIdorIp)?.netUser) != null)
                return netUser;
            return null;
        }

        /// <summary>
        /// Returns the playerID for the specified player as a string
        /// </summary>
        /// <param name="netUser"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromPlayer")]
        public string UserIDFromPlayer(NetUser netUser) => netUser.userID.ToString();

        /// <summary>
        /// Returns the playerID for the specified player as a string
        /// </summary>
        /// <param name="netUser"></param>
        /// <returns></returns>
        [LibraryFunction("IdFromPlayer")]
        public string IdFromPlayer(NetUser netUser) => netUser.userID.ToString();

        /// <summary>
        /// Returns an array with all online players' NetUser
        /// </summary>
        [LibraryFunction("GetAllNetUsers")]
        public NetUser[] GetAllNetUsers() => Enumerable.ToArray(PlayerClient.All.Select(playerClient => playerClient.netUser));

        /// <summary>
        /// Shows a notice to the player
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="message"></param>
        /// <param name="icon"></param>
        /// <param name="duration"></param>
        [LibraryFunction("Notice")]
        public void Notice(NetUser netUser, string message, string icon = " ", float duration = 4f)
        {
            Rust.Notice.Popup(netUser.networkPlayer, icon, message, duration);
        }

        /// <summary>
        /// Shows an inventory notice to the player
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="message"></param>
        [LibraryFunction("InventoryNotice")]
        public void InventoryNotice(NetUser netUser, string message) => Rust.Notice.Inventory(netUser.networkPlayer, message);

        /// <summary>
        /// Returns an Inventory.Slot.Preference
        /// </summary>
        /// <param name="kind"></param>
        /// <param name="stack"></param>
        /// <param name="flags"></param>
        [LibraryFunction("InventorySlotPreference")]
        public Inventory.Slot.Preference InventorySlotPreference(Inventory.Slot.Kind kind, bool stack, Inventory.Slot.KindFlags flags)
        {
            return Inventory.Slot.Preference.Define(kind, stack, flags);
        }

        /// <summary>
        /// Returns the character of the player
        /// </summary>
        /// <param name="netUser"></param>
        [LibraryFunction("GetCharacter")]
        public Character GetCharacter(NetUser netUser) => RustLegacyCore.GetCharacter(netUser);

        /// <summary>
        /// Returns the inventory of the player
        /// </summary>
        /// <param name="netUser"></param>
        [LibraryFunction("GetInventory")]
        public PlayerInventory GetInventory(NetUser netUser) => RustLegacyCore.GetInventory(netUser);
    }
}
