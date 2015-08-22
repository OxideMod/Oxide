using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents a config file with a dynamic layout
    /// </summary>
    public class DynamicConfigFile : ConfigFile, IEnumerable<KeyValuePair<string, object>>
    {
        private Dictionary<string, object> _keyvalues;
        private readonly JsonSerializerSettings _settings;
        private readonly string _chroot;

        /// <summary>
        /// Initializes a new instance of the DynamicConfigFile class
        /// </summary>
        public DynamicConfigFile(string filename) : base(filename)
        {
            _keyvalues = new Dictionary<string, object>();
            _settings = new JsonSerializerSettings();
            _settings.Converters.Add(new KeyValuesConverter());
            _chroot = Interface.GetMod().InstanceDirectory;
        }

        /// <summary>
        /// Loads this config from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public override void Load(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            string source = File.ReadAllText(filename);
            _keyvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(source, _settings);
        }

        /// <summary>
        /// Loads this config from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public T ReadObject<T>(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            T customObject;
            if (Exists())
            {
                var source = File.ReadAllText(filename);
                customObject = JsonConvert.DeserializeObject<T>(source);
            }
            else
            {
                customObject = Activator.CreateInstance<T>();
                WriteObject(customObject);
            }
            return customObject;
        }

        /// <summary>
        /// Saves this config to the specified file
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            var dir = GetDirectoryName(filename);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(filename, JsonConvert.SerializeObject(_keyvalues, Formatting.Indented, _settings));
        }

        /// <summary>
        /// Saves this config to the specified file
        /// </summary>
        /// <param name="sync"></param>
        /// <param name="filename"></param>
        /// <param name="config"></param>
        public void WriteObject<T>(T config, bool sync = false, string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filename, json);
            if (sync) _keyvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, _settings);
        }

        public bool Exists(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            return File.Exists(filename);
        }

        /// <summary>
        /// Check if file path is in chroot directory
        /// </summary>
        /// <param name="filename"></param>
        private string CheckPath(string filename)
        {
            filename = SanitiseName(filename);
            var path = Path.GetFullPath(filename);
            if (!path.StartsWith(_chroot, StringComparison.Ordinal))
                throw new Exception("Only access to oxide directory!");
            return filename;
        }

        /// <summary>
        /// Makes the specified name safe for use in a filename
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string SanitiseName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            name = name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
            name = Regex.Replace(name, "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]", "_");
            name = Regex.Replace(name, @"\.+", ".");
            return name.TrimStart('.', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Gets the path only
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetDirectoryName(string name)
        {
            try
            {
                return name.Substring(0, name.LastIndexOf(Path.DirectorySeparatorChar));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Clears this config
        /// </summary>
        public void Clear()
        {
            _keyvalues.Clear();
        }

        /// <summary>
        /// Gets or sets a setting on this config by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get
            {
                object val;
                if (_keyvalues.TryGetValue(key, out val))
                    return val;
                else
                    return null;
            }
            set
            {
                _keyvalues[key] = value;
            }
        }

        /// <summary>
        /// Gets or sets a nested setting on this config by key
        /// </summary>
        /// <param name="keyLevel1"></param>
        /// <param name="keyLevel2"></param>
        /// <returns></returns>
        public object this[string keyLevel1, string keyLevel2]
        {
            get { return Get(keyLevel1, keyLevel2); }
            set { Set(keyLevel1, keyLevel2, value); }
        }

        /// <summary>
        /// Gets or sets a nested setting on this config by key
        /// </summary>
        /// <param name="keyLevel1"></param>
        /// <param name="keyLevel2"></param>
        /// <param name="keyLevel3"></param>
        /// <returns></returns>
        public object this[string keyLevel1, string keyLevel2, string keyLevel3]
        {
            get { return Get(keyLevel1, keyLevel2, keyLevel3); }
            set { Set(keyLevel1, keyLevel2, keyLevel3, value); }
        }

        /// <summary>
        /// Converts a configuration value to another type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public object ConvertValue(object value, Type destinationType)
        {
            if (!destinationType.IsGenericType) return Convert.ChangeType(value, destinationType);

            if (destinationType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var valueType = destinationType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(destinationType);
                foreach (var val in (IList)value)
                    list.Add(Convert.ChangeType(val, valueType));
                return list;
            }
            else if (destinationType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = destinationType.GetGenericArguments()[0];
                var valueType = destinationType.GetGenericArguments()[1];
                var dict = (IDictionary)Activator.CreateInstance(destinationType);
                foreach (var key in ((IDictionary)value).Keys)
                    dict.Add(Convert.ChangeType(key, keyType), Convert.ChangeType(((IDictionary)value)[key], valueType));
                return dict;
            }
            else
                throw new InvalidCastException("Generic types other than List<> and Dictionary<,> are not supported");
        }

        /// <summary>
        /// Converts a configuration value to another type and returns it as that type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public T ConvertValue<T>(object value)
        {
            return (T)ConvertValue(value, typeof(T));
        }

        /// <summary>
        /// Gets a configuration value at the specified path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public object Get(params string[] path)
        {
            if (path.Length < 1)
                throw new ArgumentException("path must not be empty");
            object val;
            if (!_keyvalues.TryGetValue(path[0], out val))
                return null;
            for (var i = 1; i < path.Length; i++)
            {
                var dict = (Dictionary<string, object>)val;
                if (!dict.TryGetValue(path[i], out val))
                    return null;
            }
            return val;
        }

        /// <summary>
        /// Gets a configuration value at the specified path and converts it to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public T Get<T>(params string[] path)
        {
            return ConvertValue<T>(Get(path));
        }

        /// <summary>
        /// Sets a configuration value at the specified path
        /// </summary>
        /// <param name="pathAndTrailingValue"></param>
        public void Set(params object[] pathAndTrailingValue)
        {
            if (pathAndTrailingValue.Length < 2)
                throw new ArgumentException("path must not be empty");
            var path = new string[pathAndTrailingValue.Length - 1];
            for (var i = 0; i < pathAndTrailingValue.Length - 1; i++)
                path[i] = (string)pathAndTrailingValue[i];
            var value = pathAndTrailingValue[pathAndTrailingValue.Length - 1];
            if (path.Length == 1)
            {
                _keyvalues[path[0]] = value;
                return;
            }
            object val;
            if (!_keyvalues.TryGetValue(path[0], out val))
                _keyvalues[path[0]] = val = new Dictionary<string, object>();
            for (var i = 1; i < path.Length - 1; i++)
                val = (((Dictionary<string,object>)val)[path[i]] = new Dictionary<string, object>());
            ((Dictionary<string, object>)val)[path[path.Length - 1]] = value;
        }

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _keyvalues.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _keyvalues.GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// A mechanism to convert a keyvalues dictionary to and from json
    /// </summary>
    public class KeyValuesConverter : JsonConverter
    {
        /// <summary>
        /// Returns if this converter can convert the specified type or not
        /// </summary>
        /// <param name="objectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<string, object>) || objectType == typeof(List<object>);
        }

        private void Throw(string message)
        {
            throw new Exception(message);
        }

        /// <summary>
        /// Reads an instance of the specified type from json
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="objectType"></param>
        /// <param name="existingValue"></param>
        /// <param name="serializer"></param>
        /// <returns></returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (objectType == typeof(Dictionary<string, object>))
            {
                // Get the dictionary to populate
                Dictionary<string, object> dict = existingValue as Dictionary<string, object> ?? new Dictionary<string, object>();
                if (reader.TokenType == JsonToken.StartArray)
                {
                    JArray.Load(reader);
                    return dict;
                }
                // Read until end of object
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    // Read property name
                    if (reader.TokenType != JsonToken.PropertyName) Throw("Unexpected token: " + reader.TokenType);
                    string propname = reader.Value as string;
                    if (!reader.Read()) Throw("Unexpected end of json");

                    // What type of object are we reading?
                    switch (reader.TokenType)
                    {
                        case JsonToken.String:
                        case JsonToken.Float:
                        case JsonToken.Boolean:
                        case JsonToken.Bytes:
                        case JsonToken.Date:
                        case JsonToken.Null:
                            dict[propname] = reader.Value;
                            break;
                        case JsonToken.Integer:
                            var value = reader.Value.ToString();
                            int result;
                            if (int.TryParse(value, out result))
                                dict[propname] = result;
                            else
                                dict[propname] = value;
                            break;
                        case JsonToken.StartObject:
                            dict[propname] = serializer.Deserialize<Dictionary<string, object>>(reader);
                            break;
                        case JsonToken.StartArray:
                            dict[propname] = serializer.Deserialize<List<object>>(reader);
                            break;
                        default:
                            Throw("Unexpected token: " + reader.TokenType);
                            break;
                    }
                }

                // Return it
                return dict;
            }
            if (objectType == typeof(List<object>))
            {
                // Get the list to populate
                List<object> list = existingValue as List<object> ?? new List<object>();

                // Read until end of array
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    // What type of object are we reading?
                    switch (reader.TokenType)
                    {
                        case JsonToken.String:
                        case JsonToken.Float:
                        case JsonToken.Boolean:
                        case JsonToken.Bytes:
                        case JsonToken.Date:
                        case JsonToken.Null:
                            list.Add(reader.Value);
                            break;
                        case JsonToken.Integer:
                            var value = reader.Value.ToString();
                            int result;
                            if (int.TryParse(value, out result))
                                list.Add(result);
                            else
                                list.Add(value);
                            break;
                        case JsonToken.StartObject:
                            list.Add(serializer.Deserialize<Dictionary<string, object>>(reader));
                            break;
                        case JsonToken.StartArray:
                            list.Add(serializer.Deserialize<List<object>>(reader));
                            break;
                        default:
                            Throw("Unexpected token: " + reader.TokenType);
                            break;
                    }
                }

                // Return it
                return list;
            }
            return existingValue;
        }

        /// <summary>
        /// Writes an instance of the specified type to json
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        /// <param name="serializer"></param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Dictionary<string, object>)
            {
                // Get the dictionary to write
                var dict = (Dictionary<string, object>) value;

                // Start object
                writer.WriteStartObject();

                // Simply loop through and serialise
                foreach (var pair in dict.OrderBy(i => i.Key))
                {
                    writer.WritePropertyName(pair.Key, true);
                    serializer.Serialize(writer, pair.Value);
                }

                // End object
                writer.WriteEndObject();
            }
            else if (value is List<object>)
            {
                // Get the list to write
                var list = (List<object>) value;

                // Start array
                writer.WriteStartArray();

                // Simply loop through and serialise
                foreach (var t in list)
                    serializer.Serialize(writer, t);

                // End array
                writer.WriteEndArray();
            }
        }
    }
}
