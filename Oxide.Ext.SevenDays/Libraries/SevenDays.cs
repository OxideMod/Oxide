using Oxide.Core.Libraries;

namespace Oxide.SevenDays.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for 7 Days to Die
    /// </summary>
    public class SevenDays : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null)
        {
            if (!GameManager.Instance) return;

            if (message != null)
                GameManager.Instance.GameMessageServer(message, name);
            else
            {
                message = name;
                GameManager.Instance.GameMessageServer(message, "SERVER");
            }
        }
    }
}
