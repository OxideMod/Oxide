using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
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

        // The provider
        private ICovalenceProvider provider;

        // The command system provider
        private ICommandSystem cmdSystem;

        /// <summary>
        /// Gets the name of the current game
        /// </summary>
        [LibraryProperty("Game")]
        public string Game => provider == null ? string.Empty : provider.GameName;

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
        /// Formats the text with markup as specified in Oxide.Core.Libraries.Covalence.Formatter
        /// into the game-specific markup language
        /// </summary>
        /// <param name="text">text to format</param>
        /// <returns>formatted text</returns>
        public string FormatText(string text) => provider.FormatText(text);

        // The logger
        private Logger logger;

        // Used to measure time spent in the command
        private Stopwatch trackStopwatch = new Stopwatch();
        private Stopwatch stopwatch = new Stopwatch();
        //private float trackStartAt;
        private float averageAt;
        private double sum;
        private int preHookGcCount;
        public double totalCommandTime { get; internal set; }

        // The depth of hook call nesting
        private int nestcount;

        /// <summary>
        /// Initializes a new instance of the Covalence class
        /// </summary>
        public Covalence()
        {
            // Get logger
            logger = Interface.Oxide.RootLogger;
        }

        /// <summary>
        /// Initializes the Covalence library
        /// </summary>
        internal void Initialize()
        {
            // Search for all provider types
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
            var candidates = new List<Type>(
                candidateSet.Where(t => t != null && t.IsClass && !t.IsAbstract && t.FindInterfaces((m, o) => m == baseType, null).Length == 1)
                );

            // Select a candidate
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
                selectedCandidate = candidates[0];

            // Create it
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

            // Create mediators
            Server = provider.CreateServer();
            Players = provider.CreatePlayerManager();
            cmdSystem = provider.CreateCommandSystemProvider();

            // Log
            logger.Write(LogType.Info, "Using Covalence provider for game '{0}'", provider.GameName);
        }

        /// <summary>
        /// Registers a command (chat + console)
        /// </summary>
        /// <param name="command"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, Plugin plugin, CommandCallback callback)
        {
            if (cmdSystem == null) return;

            try
            {
                cmdSystem.RegisterCommand(command, plugin, WrapCallback(callback, plugin));
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
        public void UnregisterCommand(string command, Plugin plugin) => cmdSystem?.UnregisterCommand(command, plugin);

        public CommandCallback WrapCallback(CommandCallback callback, Plugin plugin)
        {
            return (player, cmd, args) =>
            {
                var started_at = 0f;
                if (nestcount == 0)
                {
                    preHookGcCount = GC.CollectionCount(0);
                    started_at = Interface.Oxide.Now;
                    stopwatch.Start();
                    if (averageAt < 1) averageAt = started_at;
                }
                TrackStart();
                nestcount++;
                try
                {
                    return callback(player, cmd, args);
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException($"Failed to run command '{cmd}' on plugin '{plugin.Name} v{plugin.Version}'", ex);
                    return false;
                }
                finally
                {
                    nestcount--;
                    TrackEnd();
                    if (started_at > 0)
                    {
                        stopwatch.Stop();
                        var duration = stopwatch.Elapsed.TotalSeconds;
                        if (duration > 0.2)
                        {
                            var suffix = preHookGcCount == GC.CollectionCount(0) ? string.Empty : " [GARBAGE COLLECT]";
                            Interface.Oxide.LogWarning($"Calling command '{cmd}' on '{plugin.Name} v{plugin.Version}' took {duration * 1000:0}ms{suffix}");
                        }
                        stopwatch.Reset();
                        var total = sum + duration;
                        var ended_at = started_at + duration;
                        if (ended_at - averageAt > 10)
                        {
                            total /= ended_at - averageAt;
                            if (total > 0.2)
                            {
                                var suffix = preHookGcCount == GC.CollectionCount(0) ? string.Empty : " [GARBAGE COLLECT]";
                                Interface.Oxide.LogWarning($"Calling command '{cmd}' on '{plugin.Name} v{plugin.Version}' took average {sum * 1000:0}ms{suffix}");
                            }
                            sum = 0;
                            averageAt = 0;
                        }
                        else
                        {
                            sum = total;
                        }
                    }
                }
            };
        }

        public void TrackStart()
        {
            if (nestcount > 0) return;
            var stopwatch = trackStopwatch;
            if (stopwatch.IsRunning) return;
            stopwatch.Start();
        }

        public void TrackEnd()
        {
            if (nestcount > 0) return;
            var stopwatch = trackStopwatch;
            if (!stopwatch.IsRunning) return;
            stopwatch.Stop();
            totalCommandTime += stopwatch.Elapsed.TotalSeconds;
            stopwatch.Reset();
        }
    }
}
