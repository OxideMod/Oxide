using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;

using Oxide.Game.Hurtworld.Libraries;

namespace Oxide.Plugins
{
    public abstract class HurtworldPlugin : CSharpPlugin
    {
        protected Command cmd;
        protected Hurtworld hurtworld;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            cmd = Interface.Oxide.GetLibrary<Command>();
            hurtworld = Interface.Oxide.GetLibrary<Hurtworld>();
        }

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
