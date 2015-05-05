using System.Reflection;

using Oxide.Core;
using Oxide.Core.Plugins;

using Oxide.ReignOfKings.Libraries;

using CodeHatch.Common;
using CodeHatch.Engine.Networking;

namespace Oxide.Plugins
{
    public abstract class ReignOfKingsPlugin : CSharpPlugin
    {
        protected Command cmd;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            cmd = Interface.GetMod().GetLibrary<Command>("Command");
        }

        public override void HandleAddedToManager(PluginManager manager)
        {
            foreach (MethodInfo method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var attributes = method.GetCustomAttributes(typeof(ChatCommandAttribute), true);
                if (attributes.Length > 0)
                {
                    var attribute = attributes[0] as ChatCommandAttribute;
                    cmd.AddChatCommand(attribute.Command, this, method.Name);
                }
            }
            
            base.HandleAddedToManager(manager);
        }

        /// <summary>
        /// Print a message to a players chat log
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(Player player, string format, params object[] args)
        {
            player.SendMessage(string.Format(format, args));
        }

        /// <summary>
        /// Print a message to every players chat log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void PrintToChat(string format, params object[] args)
        {
            if (Server.PlayerCount < 1) return;
            Server.BroadcastMessage(string.Format(format, args));
        }

        /// <summary>
        /// Send a reply message in response to a chat command
        /// </summary>
        /// <param name="player"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void SendReply(Player player, string format, params object[] args)
        {
            PrintToChat(player, format, args);
        }
    }
}
