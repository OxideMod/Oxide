using System.Reflection;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Hurtworld.Libraries;

namespace Oxide.Plugins
{
    public abstract class HurtworldPlugin : CSharpPlugin
    {
        protected Command cmd = Interface.Oxide.GetLibrary<Command>();
        protected Hurtworld hurt = Interface.Oxide.GetLibrary<Hurtworld>("Hurt");

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (var method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = method.GetCustomAttributes(typeof(ConsoleCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ConsoleCommandAttribute;
                    cmd.AddConsoleCommand(attribute?.Command, this, method.Name);
                    continue;
                }

                attributes = method.GetCustomAttributes(typeof(ChatCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ChatCommandAttribute;
                    cmd.AddChatCommand(attribute?.Command, this, method.Name);
                }
            }

            base.HandleAddedToManager(manager);
        }
    }
}
