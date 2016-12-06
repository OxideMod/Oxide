using System.Reflection;
using Oxide.Core;
using Oxide.Core.Libraries;

namespace Oxide.Game.SevenDays.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions 7 Days to Die
    /// </summary>
    public class SevenDays : Library
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

        #endregion

        #region Chat

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name = "SERVER", string message = null)
        {
            GameManager.Instance?.GameMessageServer(null, EnumGameMessages.Chat, message, name, false, null, false);
        }

        #endregion
    }
}
