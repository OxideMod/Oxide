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
    /// <param name="caller"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public delegate bool CommandCallback(IPlayer caller, string cmd, string[] args);

    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public interface ICommandSystem
    {
        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        void RegisterCommand(string cmd, CommandCallback callback);

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="cmd"></param>
        void UnregisterCommand(string cmd);
    }
}
