using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ProtoBuf;
using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries
{
    public class Lang : Library
    {
        public override bool IsGlobal => false;

        // Default language
        private const string DefaultLang = "en";

        // Server and user-specific language settings
        private readonly LangData langData;

        // Language files cache
        private readonly Dictionary<string, Dictionary<string, string>> langFiles;

        // A reference to the plugin remove callbacks
        private readonly Dictionary<Plugin, Event.Callback<Plugin, PluginManager>> pluginRemovedFromManager;

        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        class LangData
        {
            public string Lang = DefaultLang;
            public readonly Dictionary<string, string> UserData = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the Lang class
        /// </summary>
        public Lang()
        {
            langFiles = new Dictionary<string, Dictionary<string, string>>();
            langData = ProtoStorage.Load<LangData>("oxide.lang") ?? new LangData();
            pluginRemovedFromManager = new Dictionary<Plugin, Event.Callback<Plugin, PluginManager>>();
            Migrate(); // This should be deleted in a few weeks. (Added Oct 12, 2016)
        }

        /// <summary>
        /// Saves all data
        /// </summary>
        private void SaveData() => ProtoStorage.Save(langData, "oxide.lang");

        /// <summary>
        /// Registers a language set for a plugin
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="plugin"></param>
        /// <param name="lang"></param>
        [LibraryFunction("RegisterMessages")]
        public void RegisterMessages(Dictionary<string, string> messages, Plugin plugin, string lang = DefaultLang)
        {
            if (messages == null || string.IsNullOrEmpty(lang) || plugin == null) return;

            var file = $"{lang}{Path.DirectorySeparatorChar}{plugin.Name}.json";
            var existingMessages = GetMessagesIntern(plugin.Name, lang);

            bool changed;
            if (existingMessages == null)
            {
                langFiles.Remove(file);
                AddLangFile(file, messages, plugin);
                changed = true;
            }
            else
            {
                changed = MergeMessages(existingMessages, messages);
                messages = existingMessages;
            }

            if (!changed) return;
            if (!Directory.Exists(Path.Combine(Interface.Oxide.LangDirectory, lang))) Directory.CreateDirectory(Path.Combine(Interface.Oxide.LangDirectory, lang));
            File.WriteAllText(Path.Combine(Interface.Oxide.LangDirectory, file), JsonConvert.SerializeObject(messages, Formatting.Indented));
        }

        /// <summary>
        /// Gets a message for the plugin in the required language
        /// </summary>
        /// <param name="key"></param>
        /// <param name="plugin"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        [LibraryFunction("GetMessage")]
        public string GetMessage(string key, Plugin plugin, string userId = null)
        {
            if (string.IsNullOrEmpty(key) || plugin == null) return key;

            var lang = GetLanguage(userId);
            var file = $"{lang}{Path.DirectorySeparatorChar}{plugin.Name}.json";

            string message;
            Dictionary<string, string> langFile;
            if (!langFiles.TryGetValue(file, out langFile))
            {
                langFile = GetMessagesIntern(plugin.Name, lang);
                if (langFile == null)
                {
                    if (!lang.Equals(langData.Lang)) return GetMessage(key, plugin);
                    langFile = GetMessagesIntern(plugin.Name);
                    return langFile.TryGetValue(key, out message) ? message : key;
                }
                AddLangFile(file, langFile, plugin);
            }

            if (langFile.TryGetValue(key, out message)) return message;

            return !lang.Equals(langData.Lang) ? GetMessage(key, plugin) : key;
        }

        /// <summary>
        /// Gets all messages for a plugin in a language
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetMessages")]
        public Dictionary<string, string> GetMessages(string lang, Plugin plugin)
        {
            if (string.IsNullOrEmpty(lang) || plugin == null) return null;

            var file = $"{lang}{Path.DirectorySeparatorChar}{plugin.Name}.json";

            Dictionary<string, string> langFile;
            if (!langFiles.TryGetValue(file, out langFile))
            {
                langFile = GetMessagesIntern(plugin.Name, lang);
                if (langFile == null) return null;
                AddLangFile(file, langFile, plugin);
            }

            return langFile.ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Gets the language for a user, fall back to the default server language if no language is set
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [LibraryFunction("GetLanguage")]
        public string GetLanguage(string userId)
        {
            string lang;
            if (!string.IsNullOrEmpty(userId) && langData.UserData.TryGetValue(userId, out lang)) return lang;

            return langData.Lang;
        }

        /// <summary>
        /// Sets the language for a user
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="userId"></param>
        [LibraryFunction("SetLanguage")]
        public void SetLanguage(string lang, string userId)
        {
            if (string.IsNullOrEmpty(lang) || string.IsNullOrEmpty(userId)) return;

            string currentLang;
            if (langData.UserData.TryGetValue(userId, out currentLang) && lang.Equals(currentLang)) return;

            if (lang.Equals(langData.Lang)) langData.UserData.Remove(userId);
            else langData.UserData[userId] = lang;
            SaveData();
        }

        /// <summary>
        /// Gets the default language for the server
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetServerLanguage")]
        public string GetServerLanguage() => langData.Lang;

        /// <summary>
        /// Sets the default language for the server
        /// </summary>
        /// <param name="lang"></param>
        [LibraryFunction("SetServerLanguage")]
        public void SetServerLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang) || lang.Equals(langData.Lang)) return;

            langData.Lang = lang;
            SaveData();
        }

        /// <summary>
        /// Gets all the available languagues for a single plugin
        /// </summary>
        /// <param name="plugin"></param>
        /// <returns></returns>
        [LibraryFunction("GetLanguages")]
        public string[] GetLanguages(Plugin plugin)
        {
            var languages = new List<string>();

            if (plugin == null) return languages.ToArray();

            foreach (var directory in Directory.GetDirectories(Interface.Oxide.LangDirectory))
            {
                if (File.Exists(Path.Combine(directory, $"{plugin.Name}.json")))
                    languages.Add(directory.Substring(Interface.Oxide.LangDirectory.Length + 1));
            }

            return languages.ToArray();
        }

        /// <summary>
        /// Caches the filename and attaches the plugin remove callback
        /// </summary>
        /// <param name="file"></param>
        /// <param name="langFile"></param>
        /// <param name="plugin"></param>
        private void AddLangFile(string file, Dictionary<string, string> langFile, Plugin plugin)
        {
            langFiles.Add(file, langFile);
            if (plugin != null && !pluginRemovedFromManager.ContainsKey(plugin))
                pluginRemovedFromManager[plugin] = plugin.OnRemovedFromManager.Add(plugin_OnRemovedFromManager);
        }

        /// <summary>
        /// Update an existing language file by adding new keys and removing old keys
        /// </summary>
        /// <param name="existingMessages"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        private bool MergeMessages(Dictionary<string, string> existingMessages, Dictionary<string, string> messages)
        {
            var changed = false;

            // Check for new keys
            foreach (var message in messages)
            {
                if (existingMessages.ContainsKey(message.Key)) continue;
                existingMessages.Add(message.Key, message.Value);
                changed = true;
            }

            // Check for old keys
            foreach (var message in existingMessages.Keys.ToArray())
            {
                if (messages.ContainsKey(message)) continue;
                existingMessages.Remove(message);
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Loads a specific language file for a plugin
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetMessagesIntern(string plugin, string lang = DefaultLang)
        {
            if (string.IsNullOrEmpty(plugin)) return null;

            var file = $"{lang}{Path.DirectorySeparatorChar}{plugin}.json";
            var filename = Path.Combine(Interface.Oxide.LangDirectory, file);

            return File.Exists(filename) ? JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filename)) : null;
        }

        /// <summary>
        /// Called when the plugin was unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="manager"></param>
        private void plugin_OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            Event.Callback<Plugin, PluginManager> callback;
            if (pluginRemovedFromManager.TryGetValue(sender, out callback))
            {
                callback.Remove();
                pluginRemovedFromManager.Remove(sender);
            }

            var langs = GetLanguages(sender);

            foreach (var lang in langs) langFiles.Remove($"{lang}{Path.DirectorySeparatorChar}{sender.Name}.json");
        }

        /// <summary>
        /// Migrates the language files to the new folder structure
        /// This should be deleted in a few weeks. (Added Oct 12, 2016)
        /// </summary>
        public void Migrate()
        {
            var files = Directory.GetFiles(Interface.Oxide.LangDirectory, "*.json");

            foreach (var file in files)
            {
                var split = file.Substring(Interface.Oxide.LangDirectory.Length + 1).Split('.');
                var newPath = Path.Combine(Interface.Oxide.LangDirectory, split[1]);
                var newFile = Path.Combine(newPath, $"{split[0]}.json");

                try
                {
                    Directory.CreateDirectory(newPath);
                    if (!File.Exists(newFile)) File.Move(file, newFile);
                    else File.Delete(file);
                }
                catch (Exception ex)
                {
                    Interface.Oxide.LogException("Migrating language files to the new structure failed", ex);
                }
            }
        }
    }
}
