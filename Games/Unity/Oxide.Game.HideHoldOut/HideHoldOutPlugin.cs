using Oxide.Core;

using Oxide.Game.HideHoldOut.Libraries;

namespace Oxide.Plugins
{
    public abstract class HideHoldOutPlugin : CSharpPlugin
    {
        protected Command cmd;
        protected HideHoldOut h2o;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            cmd = Interface.Oxide.GetLibrary<Command>();
            h2o = Interface.Oxide.GetLibrary<HideHoldOut>("H2o");
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
