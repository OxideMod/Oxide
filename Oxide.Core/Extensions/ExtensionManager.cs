using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Oxide.Core.Libraries;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Core.Extensions
{
    /// <summary>
    /// Responsible for managing all Oxide extensions
    /// </summary>
    public sealed class ExtensionManager
    {
        // All loaded extensions
        private IList<Extension> extensions;

        // The core list and search patterns for extensions
        private readonly string[] coreExtensions =
        {
            "Oxide.Core.CSharp.dll",
            "Oxide.Core.JavaScript.dll",
            "Oxide.Core.Lua.dll",
            "Oxide.Core.MySql.dll",
            "Oxide.Core.Python.dll",
            "Oxide.Core.SQLite.dll",
            "Oxide.Core.Unity.dll"
        };
        private const string communitySearchPattern = "Oxide.Ext.*.dll";
        private const string gameExtSearchPattern = "Oxide.Game.*.dll";

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
        public void RegisterPluginLoader(PluginLoader loader) => pluginloaders.Add(loader);

        /// <summary>
        /// Gets all plugin loaders
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginLoader> GetPluginLoaders() => pluginloaders;

        /// <summary>
        /// Registers the specified library
        /// </summary>
        /// <param name="name"></param>
        /// <param name="library"></param>
        public void RegisterLibrary(string name, Library library)
        {
            if (libraries.ContainsKey(name))
                Interface.Oxide.LogError("An extension tried to register an already registered library: " + name);
            else
                libraries[name] = library;
        }

        /// <summary>
        /// Gets all library names
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetLibraries() => libraries.Keys;

        /// <summary>
        /// Gets the library by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Library GetLibrary(string name)
        {
            Library lib;
            return !libraries.TryGetValue(name, out lib) ? null : lib;
        }

        /// <summary>
        /// Registers the specified watcher
        /// </summary>
        /// <param name="watcher"></param>
        public void RegisterPluginChangeWatcher(PluginChangeWatcher watcher) => changewatchers.Add(watcher);

        /// <summary>
        /// Gets all plugin change watchers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginChangeWatcher> GetPluginChangeWatchers() => changewatchers;

        #endregion

        /// <summary>
        /// Loads the extension at the specified filename
        /// </summary>
        /// <param name="filename"></param>
        public void LoadExtension(string filename)
        {
            var name = Utility.GetFileNameWithoutExtension(filename);
            try
            {
                // Load the assembly
                var assembly = Assembly.LoadFile(filename);

                // Search for a type that derives Extension
                var exttype = typeof(Extension);
                Type extensiontype = null;
                foreach (var type in assembly.GetExportedTypes())
                {
                    if (!exttype.IsAssignableFrom(type)) continue;
                    extensiontype = type;
                    break;
                }
                if (extensiontype == null)
                {
                    Logger.Write(LogType.Error, "Failed to load extension {0} ({1})", name, "Specified assembly does not implement an Extension class");
                    return;
                }

                // Create and register the extension
                var extension = Activator.CreateInstance(extensiontype, this) as Extension;
                if (extension != null)
                {
                    extension.Load();
                    extensions.Add(extension);

                    // Log extension loaded
                    Logger.Write(LogType.Info, "Loaded extension {0} v{1} by {2}", extension.Name, extension.Version, extension.Author);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteException($"Failed to load extension {name}", ex);
                RemoteLogger.Exception($"Failed to load extension {name}", ex);
            }
        }

        /// <summary>
        /// Loads all extensions in the given directory
        /// </summary>
        /// <param name="directory"></param>
        public void LoadAllExtensions(string directory)
        {
            for (var i = 0; i < coreExtensions.Length; i++)
                if (File.Exists(Path.Combine(directory, coreExtensions[i]))) coreExtensions[i] = Path.Combine(directory, coreExtensions[i]);
            var gameExtensions = Directory.GetFiles(directory, gameExtSearchPattern);
            var allExtensions = Directory.GetFiles(directory, communitySearchPattern).Concat(gameExtensions).Concat(coreExtensions);

            foreach (var ext in allExtensions)
            {
                if (ext.Contains("Luma") || ext.Contains("Steam") || ext.Contains("Thief")) continue;
                if (ext.Contains(".Ext.") && Array.IndexOf(coreExtensions, ext.Replace(".Ext.", ".Core.")) != -1)
                {
                    Cleanup.Add(ext);
                    continue;
                }
                LoadExtension(Path.Combine(directory, ext));
            }

            foreach (var ext in extensions.ToArray())
            {
                try
                {
                    ext.OnModLoad();
                }
                catch (Exception ex)
                {
                    extensions.Remove(ext);
                    Logger.WriteException($"Failed OnModLoad extension {ext.Name} v{ext.Version}", ex);
                    RemoteLogger.Exception($"Failed OnModLoad extension {ext.Name} v{ext.Version}", ex);
                }
            }
        }

        /// <summary>
        /// Gets all currently loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Extension> GetAllExtensions() => extensions;

        /// <summary>
        /// Returns if an extension by the given name is present
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsExtensionPresent(string name) => extensions.Any(e => e.Name == name);

        /// <summary>
        /// Gets the extension by the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Extension GetExtension(string name)
        {
            try
            {
                return extensions.Single(e => e.Name == name);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
