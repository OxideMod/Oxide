﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using Oxide.Core.Libraries;

namespace Oxide.Core.Extensions
{
    /// <summary>
    /// Responsible for managing all Oxide extensions
    /// </summary>
    public sealed class ExtensionManager
    {
        // All loaded extensions
        private IList<Extension> extensions;

        // The search pattern for extensions
        private const string FileSearchPattern = "Oxide.Ext.*.dll";

        /// <summary>
        /// Gets the logger to which this extension manager writes
        /// </summary>
        public CompoundLogger Logger { get; private set; }

        // All registered plugin loaders
        private IList<PluginLoader> pluginloaders;

        // All registered libraries
        private IDictionary<string, Library> libraries;

        // All registered watchers
        private IList<PluginChangeWatcher> changewatchers;

        /// <summary>
        /// Initializes a new instance of the ExtensionManager class
        /// </summary>
        public ExtensionManager(CompoundLogger logger)
        {
            // Initialize
            Logger = logger;
            extensions = new List<Extension>();
            pluginloaders = new List<PluginLoader>();
            libraries = new Dictionary<string, Library>();
            changewatchers = new List<PluginChangeWatcher>();
        }

        #region Registering

        /// <summary>
        /// Registers the specified plugin loader
        /// </summary>
        /// <param name="loader"></param>
        public void RegisterPluginLoader(PluginLoader loader)
        {
            pluginloaders.Add(loader);
        }

        /// <summary>
        /// Gets all plugin loaders
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginLoader> GetPluginLoaders()
        {
            return pluginloaders;
        }

        /// <summary>
        /// Registers the specified library
        /// </summary>
        /// <param name="name"></param>
        /// <param name="library"></param>
        public void RegisterLibrary(string name, Library library)
        {
            libraries.Add(name, library);
        }

        /// <summary>
        /// Gets all library names
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetLibraries()
        {
            return libraries.Keys;
        }

        /// <summary>
        /// Gets the library by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Library GetLibrary(string name)
        {
            Library lib;
            if (!libraries.TryGetValue(name, out lib)) return null;
            return lib;
        }

        /// <summary>
        /// Registers the specified watcher
        /// </summary>
        /// <param name="watcher"></param>
        public void RegisterPluginChangeWatcher(PluginChangeWatcher watcher)
        {
            changewatchers.Add(watcher);
        }

        /// <summary>
        /// Gets all plugin change watchers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginChangeWatcher> GetPluginChangeWatchers()
        {
            return changewatchers;
        }

        #endregion

        /// <summary>
        /// Loads the extension at the specified filename
        /// </summary>
        /// <param name="filename"></param>
        public void LoadExtension(string filename)
        {
            string name = Path.GetFileNameWithoutExtension(filename);
            try
            {
                // Load the assembly
                Assembly assembly = Assembly.LoadFile(filename);

                // Search for a type that derives Extension
                Type exttype = typeof(Extension);
                Type extensiontype = null;
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (exttype.IsAssignableFrom(type))
                    {
                        extensiontype = type;
                        break;
                    }
                }
                if (extensiontype == null)
                {
                    Logger.Write(LogType.Error, "Failed to load extension {0} ({1})", name, "Specified assembly does not implement an Extension class");
                    return;
                }

                // Create and register the extension
                Extension extension = Activator.CreateInstance(extensiontype, new object[1] { this }) as Extension;
                extensions.Add(extension);
                extension.Load();

                // Log extension loaded
                Logger.Write(LogType.Info, "Loaded extension {0} v{1} by {2}", extension.Name, extension.Version, extension.Author);
            }
            catch (Exception ex)
            {
                //Logger.Write(LogType.Error, "Failed to load extension {0} ({1})", name, ex.Message);
                Logger.WriteException(string.Format("Failed to load extension {0}", name), ex);
            }
        }

        /// <summary>
        /// Loads all extensions in the given directory
        /// </summary>
        /// <param name="directory"></param>
        public void LoadAllExtensions(string directory)
        {
            string[] files = Directory.GetFiles(directory, FileSearchPattern);
            foreach (string file in files)
            {
                LoadExtension(Path.Combine(directory, file));
            }
            foreach (Extension ext in extensions)
                ext.OnModLoad();
        }

        /// <summary>
        /// Gets all currently loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Extension> GetAllExtensions()
        {
            return extensions;
        }

        /// <summary>
        /// Returns if an extension by the given name is present
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsExtensionPresent(string name)
        {
            return extensions.Any((e) => e.Name == name);
        }

        /// <summary>
        /// Gets the extension by the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Extension GetExtension(string name)
        {
            try
            {
                return extensions.Single((e) => e.Name == name);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
