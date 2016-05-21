﻿using System;
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
        public string Game => provider == null ? string.Empty : provider.GameName;

        // The logger
        private Logger logger;

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
                logger.Write(LogType.Warning, "No Covalence providers found, Covalence will not be functional for this session.");
                return;
            }
            var candidates = new List<Type>(
                candidateSet.Where(t => t != null && t.IsClass && !t.IsAbstract && t.FindInterfaces((m, o) => m == baseType, null).Length == 1)
                );

            // Select a candidate
            Type selectedCandidate;
            if (candidates.Count == 0)
            {
                logger.Write(LogType.Warning, "No Covalence providers found, Covalence will not be functional for this session.");
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
                provider = (ICovalenceProvider) Activator.CreateInstance(selectedCandidate);
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
        /// <param name="command"></param>
        /// <param name="callback"></param>
        public void RegisterCommand(string command, CommandCallback callback)
        {
            if (cmdSystem == null) return;

            try
            {
                cmdSystem.RegisterCommand(command, callback);
            }
            catch (CommandAlreadyExistsException)
            {
                logger.Write(LogType.Error, "Tried to register command '{0}', already exists!", command);
            }
        }

        /// <summary>
        /// Unregisters a command (chat + console)
        /// </summary>
        /// <param name="command"></param>
        public void UnregisterCommand(string command) => cmdSystem?.UnregisterCommand(command);
    }
}
