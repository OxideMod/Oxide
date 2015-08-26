using System.Linq;
using System.Reflection;

using Oxide.Core.Libraries;

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
        public override bool IsGlobal => false;

        /// <summary>
        /// Returns the UserID for the specified player as a string
        /// </summary>
        /// <param name="netuser"></param>
        /// <returns></returns>
        [LibraryFunction("UserIDFromPlayer")]
        public string UserIDFromPlayer(NetUser netuser)
        {
            return netuser.userID.ToString();
        }

        /// <summary>
        /// Print a message to every players chat log
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null)
        {
            if (message == null)
            {
                message = name;
                name = "Server";
            }
            ConsoleNetworker.Broadcast($"chat.add {QuoteSafe(name)} {QuoteSafe(message)}");
        }

        /// <summary>
        /// Sends a chat message to the user
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(NetUser netUser, string name, string message = null)
        {
            if (message == null)
            {
                message = name;
                name = "Server";
            }
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, $"chat.add {QuoteSafe(name)} {QuoteSafe(message)}");
        }

        /// <summary>
        /// Print a message to a players console log
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [LibraryFunction("SendConsoleMessage")]
        public void SendConsoleMessage(NetUser netUser, string format, params object[] args)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, $"echo {QuoteSafe(string.Format(format, args))}");
        }

        /// <summary>
        /// Print a message to every players console log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        [LibraryFunction("BroadcastConsole")]
        public void BroadcastConsole(string format, params object[] args)
        {
            ConsoleNetworker.Broadcast($"echo {QuoteSafe(string.Format(format, args))}");
        }

        /// <summary>
        /// Runs a console command on the server
        /// </summary>
        /// <param name="cmd"></param>
        [LibraryFunction("RunServerCommand")]
        public void RunServerCommand(string cmd)
        {
            ConsoleSystem.Run(cmd);
        }

        /// <summary>
        /// Runs a console command for a specific player
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="cmd"></param>
        [LibraryFunction("RunClientCommand")]
        public void RunClientCommand(NetUser netUser, string cmd)
        {
            ConsoleNetworker.SendClientCommand(netUser.networkPlayer, cmd);
        }

        /// <summary>
        /// Gets private bindingflag for accessing private methods, fields, and properties
        /// </summary>
        [LibraryFunction("PrivateBindingFlag")]
        public BindingFlags PrivateBindingFlag()
        {
            return (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
        }

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="str"></param>
        [LibraryFunction("QuoteSafe")]
        public string QuoteSafe(string str) => "\"" + str.Replace("\"", "\\\"").TrimEnd('\\') + "\"";

        /// <summary>
        /// Finds a player by name, steam id or ip
        /// </summary>
        /// <param name="strNameOrIdorIp"></param>
        [LibraryFunction("FindPlayer")]
        public NetUser FindPlayer(string strNameOrIdorIp)
        {
            NetUser netUser;
            if ((netUser = PlayerClient.All.Find(p => p.netUser.userID.ToString() == strNameOrIdorIp)?.netUser) != null)
            {
                return netUser;
            }
            if ((netUser = PlayerClient.All.Find(p => p.netUser.displayName.ToLower().Contains(strNameOrIdorIp.ToLower()))?.netUser) != null)
            {
                return netUser;
            }
            if ((netUser = PlayerClient.All.Find(p => p.netUser.networkPlayer.ipAddress == strNameOrIdorIp)?.netUser) != null)
            {
                return netUser;
            }
            return null;
        }

        /// <summary>
        /// Returns an array with all online player's their NetUser
        /// </summary>
        [LibraryFunction("GetAllNetUsers")]
        public NetUser[] GetAllNetUsers()
        {
            return Enumerable.ToArray(PlayerClient.All.Select(playerClient => playerClient.netUser));
        }

        /// <summary>
        /// Shows a notice to a player
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
        /// Shows an inventory notice to a player
        /// </summary>
        /// <param name="netUser"></param>
        /// <param name="message"></param>
        [LibraryFunction("InventoryNotice")]
        public void InventoryNotice(NetUser netUser, string message)
        {
            Rust.Notice.Inventory(netUser.networkPlayer, message);
        }

        /// <summary>
        /// Returns an Inventory.Slot.Preference
        /// </summary>
        /// <param name="startSlotKind"></param>
        /// <param name="stack"></param>
        /// <param name="flags"></param>
        [LibraryFunction("InventorySlotPreference")]
        public Inventory.Slot.Preference InventorySlotPreference(Inventory.Slot.Kind startSlotKind, bool stack, Inventory.Slot.KindFlags flags)
        {
            return Inventory.Slot.Preference.Define(startSlotKind, stack, flags);
        }

        [LibraryFunction("GetCharacter")]
        public Character GetCharacter(NetUser netUser) => RustLegacyCore.GetCharacter(netUser);

        [LibraryFunction("GetInventory")]
        public PlayerInventory GetInventory(NetUser netUser) => RustLegacyCore.GetInventory(netUser);
    }
}
