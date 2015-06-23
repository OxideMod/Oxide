using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Oxide.Core.Logging;
using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// The Covalence library
    /// </summary>
    public class Covalence : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        /// <summary>
        /// Gets the server mediator
        /// </summary>
        [LibraryProperty("Server")]
        public IServer Server { get; private set; }

        /// <summary>
        /// Gets the player manager mediator
        /// </summary>
        [LibraryProperty("Players")]
        public IPlayerManager Players { get; private set; }

        // The provider
        private ICovalenceProvider provider;

        // The command system provider
        private ICommandSystem cmdSystem;

        // Registered commands
        private class RegisteredCommand
        {
            /// <summary>
            /// The plugin that handles the command
            /// </summary>
            public readonly Plugin Source;

            /// <summary>
            /// The name of the command
            /// </summary>
            public readonly string Command;

            /// <summary>
            /// The name of the callback
            /// </summary>
            public readonly string Callback;

            /// <summary>
            /// Initializes a new instance of the RegisteredCommand class
            /// </summary>
            /// <param name="source"></param>
            /// <param name="command"></param>
            /// <param name="callback"></param>
            public RegisteredCommand(Plugin source, string command, string callback)
            {
                // Store fields
                Source = source;
                Command = command;
                Callback = callback;
            }
        }
        private IDictionary<string, RegisteredCommand> commands;

        /// <summary>
        /// Gets the name of the current game
        /// </summary>
        [LibraryProperty("Game")]
        public string Game
        {
            get
            {
                if (provider == null) return string.Empty;
                return provider.GameName;
            }
        }

        // The logger
        private Logger logger;

        /// <summary>
        /// Initializes a new instance of the Covalence class
        /// </summary>
        public Covalence()
        {
            // Get logger
            logger = Interface.GetMod().RootLogger;
        }

        /// <summary>
        /// Initializes the Covalence library
        /// </summary>
        internal void Initialize()
        {
            // Search for all provider types
            Type baseType = typeof(ICovalenceProvider);
            IEnumerable<Type> candidateSet = null;
            foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] assTypes = null;
                try
                {
                    assTypes = ass.GetTypes();
                }
                catch (ReflectionTypeLoadException rtlEx)
                {
                    assTypes = rtlEx.Types;
                }
                catch (TypeLoadException tlEx)
                {
                    logger.Write(LogType.Warning, "Covalence: Type {0} could not be loaded from assembly '{1}': {2}", tlEx.TypeName, ass.FullName, tlEx);
                }
                if (assTypes != null)
                {
                    if (candidateSet == null)
                        candidateSet = assTypes;
                    else
                        candidateSet = candidateSet.Concat(assTypes);
                }
            }
            if (candidateSet == null)
            {
                logger.Write(LogType.Warning, "No Covalence providers found, Covalence will not be functional for this session.");
                return;
            }
            List<Type> candidates = new List<Type>(
                candidateSet.Where((t) => t.IsClass && !t.IsAbstract && t.FindInterfaces((m, o) => m == baseType, null).Length == 1)
                );

            // Select a candidate
            Type selectedCandidate;
            if (candidates.Count == 0)
            {
                logger.Write(LogType.Warning, "No Covalence providers found, Covalence will not be functional for this session.");
                return;
            }
            else if (candidates.Count > 1)
            {
                selectedCandidate = candidates[0];
                StringBuilder sb = new StringBuilder();
                for (int i = 1; i < candidates.Count; i++)
                {
                    if (i > 1) sb.Append(',');
                    sb.Append(candidates[i].FullName);
                }
                logger.Write(LogType.Warning, "Multiple Covalence providers found! Using {0}. (Also found {1})");
            }
            else
                selectedCandidate = candidates[0];

            // Create it
            try
            {
                provider = Activator.CreateInstance(selectedCandidate) as ICovalenceProvider;
            }
            catch (Exception ex)
            {
                logger.Write(LogType.Warning, "Got exception when instantiating Covalence provider, Covalence will not be functional for this session.");
                logger.Write(LogType.Warning, "{0}", ex);
                return;
            }

            // Create mediators
            Server = provider.CreateServer();
            Players = provider.CreatePlayerManager();
            cmdSystem = provider.CreateCommandSystemProvider();

            // Initialize other things
            commands = new Dictionary<string, RegisteredCommand>();

            // Log
            logger.Write(LogType.Info, "Using Covalence provider for game '{0}'", provider.GameName);
        }

        /// <summary>
        /// Registers a command (chat + console)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string cmd, CommandCallback callback)
        {
            try
            {
                cmdSystem.RegisterCommand(cmd, CommandType.Chat, callback);
                cmdSystem.RegisterCommand(cmd, CommandType.Console, callback);
            }
            catch (CommandAlreadyExistsException)
            {
                logger.Write(LogType.Error, "Tried to register command '{0}', already exists!", cmd);
            }
        }

        /// <summary>
        /// Unregisters a command (chat + console)
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="callback"></param>
        public void UnregisterCommand(string cmd)
        {
            logger.Write(LogType.Debug, "Covalence is unregistering command '{0}'!", cmd);
            cmdSystem.UnregisterCommand(cmd, CommandType.Chat);
            cmdSystem.UnregisterCommand(cmd, CommandType.Console);
        }

    }
}
