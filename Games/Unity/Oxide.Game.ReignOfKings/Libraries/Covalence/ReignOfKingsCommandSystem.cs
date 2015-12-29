using CodeHatch.Engine.Core.Commands;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    public class ReignOfKingsCommandSystem : ICommandSystem
    {
        /// <summary>
        /// Registers the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string cmd, CommandType type, CommandCallback callback)
        {
            // Is it a console command?
            if (type == CommandType.Console) return;

            // Convert to lowercase
            var commandName = cmd.ToLowerInvariant();

            // Check if it already exists
            if (CommandManager.RegisteredCommands.ContainsKey(commandName)) throw new CommandAlreadyExistsException(commandName);

            // Register it
            var commandAttribute = new CommandAttribute($"/{commandName}", string.Empty)
            {
                Method = info =>
                {
                    var player = ReignOfKingsCovalenceProvider.Instance.PlayerManager.GetPlayer(info.PlayerId.ToString());
                    callback(info.Label, CommandType.Chat, player, info.Args);
                }
            };
            CommandManager.RegisteredCommands[commandName] = commandAttribute;
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        public void UnregisterCommand(string cmd, CommandType type) => CommandManager.RegisteredCommands.Remove(cmd);
    }
}
