using System;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a source of commands
    /// </summary>
    public enum CommandType { Chat, Console }

    /// <summary>
    /// Represents the callback of a chat or console command
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="type"></param>
    /// <param name="caller"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public delegate bool CommandCallback(string cmd, CommandType type, IPlayer caller, string[] args);

    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public interface ICommandSystem
    {
        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        void RegisterCommand(string cmd, CommandType type, CommandCallback callback);

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        void UnregisterCommand(string cmd, CommandType type);
    }
}
