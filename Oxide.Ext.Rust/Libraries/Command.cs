using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Rust.Libraries
{
    /// <summary>
    /// A library containing functions for adding console and chat commands
    /// </summary>
    public class Command : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        private struct ConsoleCommand
        {
            public string Name;
            public Plugin Plugin;
            public string CallbackName;
            public ConsoleSystem.Command RustCommand;
        }

        private struct ChatCommand
        {
            public string Name;
            public Plugin Plugin;
            public string CallbackName;
        }

        // All console commands that we're currently tracking
        private IList<ConsoleCommand> concommands;

        // Rust's internal command dictionary
        private IDictionary<string, ConsoleSystem.Command> rustcommands;

        // All chat commands that we're currently tracking
        private IDictionary<string, ChatCommand> chatcommands;

        /// <summary>
        /// Initialises a new instance of the Command class
        /// </summary>
        public Command()
        {
            // Initialise
            concommands = new List<ConsoleCommand>();
            chatcommands = new Dictionary<string, ChatCommand>();
        }

        /// <summary>
        /// Adds a console command
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        /// <param name="callbackname"></param>
        [LibraryFunction("AddConsoleCommand")]
        public void AddConsoleCommand(string name, Plugin plugin, string callbackname)
        {
            // Hack us the dictionary
            if (rustcommands == null) rustcommands = typeof(ConsoleSystem.Index).GetField("dictionary", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as IDictionary<string, ConsoleSystem.Command>;

            // Split into parent and field name
            string[] leftright = name.Trim().Split('.');

            // Create the command struct
            ConsoleCommand cmd = new ConsoleCommand();
            cmd.Name = name;
            cmd.Plugin = plugin;
            cmd.CallbackName = callbackname;
            cmd.RustCommand = new ConsoleSystem.Command
            {
                name = leftright[1],
                parent = leftright[0],
                namefull = string.Join(".", leftright),
                isCommand = true,
                isAdmin = true,
                GetString = ReturnEmptyString,
                SetString = DoNothing,
                Call = CallCommand
            };

            // Add to collections
            concommands.Add(cmd);
            rustcommands.Add(cmd.RustCommand.namefull, cmd.RustCommand);
            ConsoleSystem.Index.GetAll().Add(cmd.RustCommand);

            // Hook the unload event
            plugin.OnRemovedFromManager += plugin_OnRemovedFromManager;
        }

        /// <summary>
        /// Adds a chat command
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        /// <param name="callbackname"></param>
        [LibraryFunction("AddChatCommand")]
        public void AddChatCommand(string name, Plugin plugin, string callbackname)
        {
            // Create the command struct
            ChatCommand cmd = new ChatCommand();
            cmd.Name = name.ToLowerInvariant();
            cmd.Plugin = plugin;
            cmd.CallbackName = callbackname;

            // Add to collections
            chatcommands.Add(cmd.Name, cmd);

            // Hook the unload event
            plugin.OnRemovedFromManager += plugin_OnRemovedFromManager;
        }

        /// <summary>
        /// Handles the specified chat command
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        internal bool HandleChatCommand(BasePlayer sender, string name, string[] args)
        {
            // Try and find it
            ChatCommand cmd;
            if (!chatcommands.TryGetValue(name.ToLowerInvariant(), out cmd)) return false;

            // Call it
            cmd.Plugin.CallHook(cmd.CallbackName, new object[] { sender, name, args });

            // Handled
            return true;
        }

        /// <summary>
        /// Calls the specified command
        /// </summary>
        /// <param name="arg"></param>
        private void CallCommand(ConsoleSystem.Arg arg)
        {
            //Oxide.Core.Interface.GetMod().RootLogger.Write(Core.Logging.LogType.Debug, "CallCommand {0}", arg.cmd.namefull);

            // Find the command
            ConsoleCommand cmd;
            try
            {
                cmd = concommands.Single((c) => c.RustCommand == arg.cmd);
            }
            catch (Exception)
            {
                //Oxide.Core.Interface.GetMod().RootLogger.Write(Core.Logging.LogType.Debug, "Command not found");
                return;
            }

            // Execute it
            cmd.Plugin.CallHook(cmd.CallbackName, new object[] { arg });
        }

        #region Empty Functions

        private string ReturnEmptyString()
        {
            return string.Empty;
        }

        private void DoNothing(string str)
        {

        }

        #endregion

        /// <summary>
        /// Called when a plugin has been removed from manager
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="manager"></param>
        private void plugin_OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            // Find all console commands that belong to the plugin
            HashSet<ConsoleCommand> concommands_toremove = new HashSet<ConsoleCommand>(concommands.Where((c) => c.Plugin == sender));
            foreach (ConsoleCommand cmd in concommands_toremove)
            {
                // Remove it
                concommands.Remove(cmd);
                rustcommands.Remove(cmd.RustCommand.namefull);
                ConsoleSystem.Index.GetAll().Remove(cmd.RustCommand);

            }

            // Find all chat commands that belong to the plugin
            HashSet<ChatCommand> chatcommands_toremove = new HashSet<ChatCommand>(chatcommands.Values.Where((c) => c.Plugin == sender));
            foreach (ChatCommand cmd in chatcommands_toremove)
            {
                // Remove it
                chatcommands.Remove(cmd.Name);
            }

            // Unhook the event
            sender.OnRemovedFromManager -= plugin_OnRemovedFromManager;
        }
    }
}
