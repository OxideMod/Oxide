using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// The Covalence library
    /// </summary>
    public class Covalence : Library
    {
        private ICommandSystem cmdSystem;
        private ICovalenceProvider provider;
        private readonly Logger logger;

        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

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

        /// <summary>
        /// Gets the name of the current game
        /// </summary>
        [LibraryProperty("Game")]
        public string Game => provider?.GameName ?? string.Empty;

        /// <summary>
        /// Gets the Steam app ID of the game's client, if available
        /// </summary>
        [LibraryProperty("ClientAppId")]
        public uint ClientAppId => provider?.ClientAppId ?? 0;

        /// <summary>
        /// Gets the Steam app ID of the game's server, if available
        /// </summary>
        [LibraryProperty("ServerAppId")]
        public uint ServerAppId => provider?.ServerAppId ?? 0;

        /// <summary>
        /// Formats the text with markup into the game-specific markup language
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public string FormatText(string text) => provider.FormatText(text);

        /// <summary>
        /// Initializes a new instance of the Covalence class
        /// </summary>
        public Covalence()
        {
            logger = Interface.Oxide.RootLogger;
        }

        /// <summary>
        /// Initializes the Covalence library
        /// </summary>
        internal void Initialize()
        {
            var baseType = typeof(ICovalenceProvider);
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
                    candidateSet = candidateSet?.Concat(assTypes) ?? assTypes;
            }
            if (candidateSet == null)
            {
                logger.Write(LogType.Warning, "Covalence not available yet for this game");
                return;
            }
            var candidates = new List<Type>(candidateSet.Where(t => t != null && t.IsClass && !t.IsAbstract && t.FindInterfaces((m, o) => m == baseType, null).Length == 1));

            Type selectedCandidate;
            if (candidates.Count == 0)
            {
                logger.Write(LogType.Warning, "Covalence not available yet for this game");
                return;
            }
            if (candidates.Count > 1)
            {
                selectedCandidate = candidates[0];
                var sb = new StringBuilder();
                for (var i = 1; i < candidates.Count; i++)
                {
                    if (i > 1) sb.Append(',');
                    sb.Append(candidates[i].FullName);
                }
                logger.Write(LogType.Warning, "Multiple Covalence providers found! Using {0}. (Also found {1})", selectedCandidate, sb);
            }
            else
            {
                selectedCandidate = candidates[0];
            }

            try
            {
                provider = (ICovalenceProvider)Activator.CreateInstance(selectedCandidate);
            }
            catch (Exception ex)
            {
                logger.Write(LogType.Warning, "Got exception when instantiating Covalence provider, Covalence will not be functional for this session.");
                logger.Write(LogType.Warning, "{0}", ex);
                return;
            }

            Server = provider.CreateServer();
            Players = provider.CreatePlayerManager();
            cmdSystem = provider.CreateCommandSystemProvider();

            logger.Write(LogType.Info, "Using Covalence provider for game '{0}'", provider.GameName);
        }

        /// <summary>
        /// Registers a command (chat + console)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            if (cmdSystem == null) return;

            try
            {
                cmdSystem.RegisterCommand(command, plugin, callback);
            }
            catch (CommandAlreadyExistsException)
            {
                var pluginName = plugin?.Name ?? "An unknown plugin";
                logger.Write(LogType.Error, "{0} tried to register command '{1}', this command already exists and cannot be overridden!", pluginName, command);
            }
        }

        /// <summary>
        /// Unregisters a command (chat + console)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="plugin"></param>
        public void UnregisterCommand(string command, Plugin plugin) => cmdSystem?.UnregisterCommand(command, plugin);
    }
}
