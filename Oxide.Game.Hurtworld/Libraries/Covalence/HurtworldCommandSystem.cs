using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.Hurtworld.Libraries.Covalence
{
    /// <summary>
    /// Represents a binding to a generic command system
    /// </summary>
    class HurtworldCommandSystem : ICommandSystem
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
            var command_name = cmd.ToLowerInvariant();

            // Check if it already exists
            if (CommandManager.RegisteredCommands.ContainsKey(command_name))
            {
                throw new CommandAlreadyExistsException(command_name);
            }

            // Register it
            var commandAttribute = new CommandAttribute("/" + command_name, string.Empty)
            {
                Method = info =>
                {
                    var player = HurtworldCovalenceProvider.Instance.PlayerManager.GetPlayer(info.PlayerId.ToString());
                    callback(info.Label, CommandType.Chat, player, info.Args);
                }
            };
            CommandManager.RegisteredCommands[command_name] = commandAttribute;
        }

        /// <summary>
        /// Unregisters the specified command
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="type"></param>
        public void UnregisterCommand(string cmd, CommandType type)
        {
            CommandManager.RegisteredCommands.Remove(cmd);
        }
    }
}
