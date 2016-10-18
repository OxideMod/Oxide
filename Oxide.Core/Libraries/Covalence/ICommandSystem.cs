using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a source of commands
    /// </summary>
    public enum CommandType { Chat, Console }

    /// <summary>
    /// Represents the callback of a chat or console command
    /// </summary>
    /// <param name="command"></param>
    /// <param name="caller"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public delegate bool CommandCallback(IPlayer caller, string command, string[] args);

    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public interface ICommandSystem
    {
        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        void RegisterCommand(string command, Plugin plugin, CommandCallback callback);

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="command"></param>
        void UnregisterCommand(string command, Plugin plugin);
    }
}
