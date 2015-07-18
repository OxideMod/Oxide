using CodeHatch.Engine.Core.Commands;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    class ReignOfKingsCommandSystem : ICommandSystem
    {
        public void RegisterCommand(string cmd, CommandType type, CommandCallback callback)
        {
            if (type == CommandType.Console) return;
            var command_name = cmd.ToLowerInvariant();
            if (CommandManager.RegisteredCommands.ContainsKey(command_name))
            {
                throw new CommandAlreadyExistsException(command_name);
            }
            var commandAttribute = new CommandAttribute("/" + command_name, string.Empty)
            {
                Method = info =>
                {
                    var player = ReignOfKingsCovalenceProvider.Instance.PlayerManager.GetPlayer(info.PlayerId.ToString());
                    callback(info.Label, CommandType.Chat, player, info.Args);
                }
            };
            CommandManager.RegisteredCommands[command_name] = commandAttribute;
        }

        public void UnregisterCommand(string cmd, CommandType type)
        {
            CommandManager.RegisteredCommands.Remove(cmd);
        }
    }
}
