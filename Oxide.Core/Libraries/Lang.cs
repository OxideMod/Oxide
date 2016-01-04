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

        private readonly LangData langData;
        private readonly Dictionary<string, Dictionary<string, string>> langFiles;
        private const string DefaultLang = "en";

        [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
        class LangData
        {
            public string Lang = DefaultLang;
            public Dictionary<string, string> UserData = new Dictionary<string, string>();
        }

        public Lang()
        {
            langFiles = new Dictionary<string, Dictionary<string, string>>();
            langData = ProtoStorage.Load<LangData>("oxide.lang") ?? new LangData();
        }

        /// <summary>
        /// Saves all data
        /// </summary>
        private void SaveData() => ProtoStorage.Save(langData, "oxide.lang");

        [LibraryFunction("RegisterMessages")]
        public void RegisterMessages(Dictionary<string, string> messages, Plugin plugin, string lang = DefaultLang)
        {
            if (messages == null || string.IsNullOrEmpty(lang) || plugin == null) return;
            var file = $"{plugin.Name}.{lang}.json";
            bool changed;
            var existingMessages = GetMessagesIntern(file);
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
            if (changed)
                File.WriteAllText(Path.Combine(Interface.Oxide.LangDirectory, file), JsonConvert.SerializeObject(messages, Formatting.Indented));
        }

        [LibraryFunction("GetMessage")]
        public string GetMessage(string key, Plugin plugin, string userId = null)
        {
            if (string.IsNullOrEmpty(key) || plugin == null) return key;
            var lang = GetLanguage(userId);
            var file = $"{plugin.Name}.{lang}.json";
            Dictionary<string, string> langFile;
            if (!langFiles.TryGetValue(file, out langFile))
            {
                langFile = GetMessagesIntern(file);
                if (langFile == null)
                {
                    if (userId != null && !lang.Equals(langData.Lang)) return GetMessage(key, plugin);
                    return key;
                }
                AddLangFile(file, langFile, plugin);
            }
            string message;
            if (langFile.TryGetValue(key, out message)) return message ?? key;
            if (userId != null && !lang.Equals(langData.Lang)) return GetMessage(key, plugin);
            return key;
        }

        [LibraryFunction("GetMessages")]
        public Dictionary<string, string> GetMessages(string lang, Plugin plugin)
        {
            if (string.IsNullOrEmpty(lang) || plugin == null) return null;
            var file = $"{plugin.Name}.{lang}.json";
            Dictionary<string, string> langFile;
            if (!langFiles.TryGetValue(file, out langFile))
            {
                langFile = GetMessagesIntern(file);
                if (langFile == null) return null;
                AddLangFile(file, langFile, plugin);
            }
            return langFile.ToDictionary(k => k.Key, v => v.Value);
        }

        [LibraryFunction("GetLanguage")]
        public string GetLanguage(string userId)
        {
            string lang;
            if (!string.IsNullOrEmpty(userId) && langData.UserData.TryGetValue(userId, out lang)) return lang;
            return langData.Lang;
        }

        [LibraryFunction("SetLanguage")]
        public void SetLanguage(string lang, string userId)
        {
            if (string.IsNullOrEmpty(lang) || string.IsNullOrEmpty(userId)) return;
            string currentLang;
            if (langData.UserData.TryGetValue(userId, out currentLang) && lang.Equals(currentLang)) return;
            if (lang.Equals(langData.Lang))
                langData.UserData.Remove(userId);
            else
                langData.UserData[userId] = lang;
            SaveData();
        }

        [LibraryFunction("GetServerLanguage")]
        public string GetServerLanguage() => langData.Lang;

        [LibraryFunction("SetServerLanguage")]
        public void SetServerLanguage(string lang)
        {
            if (string.IsNullOrEmpty(lang) || lang.Equals(langData.Lang)) return;
            langData.Lang = lang;
            SaveData();
        }

        [LibraryFunction("GetLanguages")]
        public string[] GetLanguages(Plugin plugin)
        {
            var languages = new List<string>();
            if (plugin == null) return languages.ToArray();
            var files = Directory.GetFiles(Interface.Oxide.LangDirectory, $"{plugin.Name}.*.json");
            foreach (var file in files) languages.Add(file.Split('.')[1]);
            return languages.ToArray();
        }

        private void AddLangFile(string file, Dictionary<string, string> langFile, Plugin plugin)
        {
            langFiles.Add(file, langFile);
            plugin.OnRemovedFromManager -= plugin_OnRemovedFromManager;
            plugin.OnRemovedFromManager += plugin_OnRemovedFromManager;
        }

        private bool MergeMessages(Dictionary<string, string> existingMessages, Dictionary<string, string> messages)
        {
            var changed = false;
            // check for new keys
            foreach (var message in messages)
            {
                if (!existingMessages.ContainsKey(message.Key))
                {
                    existingMessages.Add(message.Key, message.Value);
                    changed = true;
                }
            }
            // check for old keys
            foreach (var message in existingMessages.Keys.ToArray())
            {
                if (!messages.ContainsKey(message))
                {
                    existingMessages.Remove(message);
                    changed = true;
                }
            }
            return changed;
        }

        private Dictionary<string, string> GetMessagesIntern(string file)
        {
            if (string.IsNullOrEmpty(file)) return null;
            var filename = Path.Combine(Interface.Oxide.LangDirectory, file);
            return !File.Exists(filename) ? null : JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filename));
        }

        /// <summary>
        /// Called when the plugin was unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="manager"></param>
        private void plugin_OnRemovedFromManager(Plugin sender, PluginManager manager)
        {
            sender.OnRemovedFromManager -= plugin_OnRemovedFromManager;
            var langs = GetLanguages(sender);
            foreach (var lang in langs) langFiles.Remove($"{sender.Name}.{lang}.json");
        }
    }
}
