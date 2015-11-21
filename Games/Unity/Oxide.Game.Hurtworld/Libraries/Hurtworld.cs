using uLink;

using Oxide.Core.Libraries;

namespace Oxide.Game.Hurtworld.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions
    /// </summary>
    public class Hurtworld : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Broadcasts a chat message
        /// </summary>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("BroadcastChat")]
        public void BroadcastChat(string name, string message = null)
        {
            if (message != null)
            {
                ChatManager.Instance?.AppendChatboxServerAll(string.Concat("<color=#6495be>", name, "</color>", message));
            }
            else
            {
                message = name;
                ChatManager.Instance?.AppendChatboxServerAll(string.Concat("<color=#b8d7a3>", message, "</color>"));
            }
        }

        /// <summary>
        /// Sends a chat message to the player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        [LibraryFunction("SendChatMessage")]
        public void SendChatMessage(NetworkPlayer player, string name, string message = null)
        {
            if (message != null)
            {
                ChatManager.Instance?.AppendChatboxServerSingle(string.Concat("<color=#6495be>", name, "</color>", message), player);
            }
            else
            {
                message = name;
                ChatManager.Instance?.AppendChatboxServerSingle(string.Concat("<color=#b8d7a3>", message, "</color>"), player);
            }
        }
    }
}
