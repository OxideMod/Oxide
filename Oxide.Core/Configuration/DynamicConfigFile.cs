using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Newtonsoft.Json;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents a config file with a dynamic layout
    /// </summary>
    public class DynamicConfigFile : ConfigFile, IEnumerable<KeyValuePair<string, object>>
    {
        private Dictionary<string, object> keyvalues;

        /// <summary>
        /// Initialises a new instance of the DynamicConfigFile class
        /// </summary>
        public DynamicConfigFile()
        {
            keyvalues = new Dictionary<string, object>();
        }

        /// <summary>
        /// Loads this config file from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public override void Load(string filename)
        {
            string source = File.ReadAllText(filename);
            KeyValuesConverter converter = new KeyValuesConverter();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);
            keyvalues = JsonConvert.DeserializeObject<Dictionary<string, object>>(source, settings);
        }

        /// <summary>
        /// Saves this config to the specified file
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            KeyValuesConverter converter = new KeyValuesConverter();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);
            File.WriteAllText(filename, JsonConvert.SerializeObject(keyvalues, Formatting.Indented, settings));
        }

        /// <summary>
        /// Gets or sets a setting on this config file by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object this[string key]
        {
            get
            {
                object val;
                if (keyvalues.TryGetValue(key, out val))
                    return val;
                else
                    return null;
            }
            set
            {
                keyvalues[key] = value;
            }
        }

        #region IEnumerable

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return keyvalues.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return keyvalues.GetEnumerator();
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
                Dictionary<string, object> dict = existingValue as Dictionary<string, object>;
                if (dict == null) dict = new Dictionary<string, object>();

                // Read until end of object
                while (reader.Read() && reader.TokenType != JsonToken.EndObject)
                {
                    // Read property name
                    if (reader.TokenType != JsonToken.PropertyName) Throw("Unexpected token");
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
                            dict[propname] = reader.Value;
                            break;
                        case JsonToken.Integer:
                            dict[propname] = Convert.ToInt32(reader.Value);
                            break;
                        case JsonToken.StartObject:
                            dict[propname] = serializer.Deserialize<Dictionary<string, object>>(reader);
                            break;
                        case JsonToken.StartArray:
                            dict[propname] = serializer.Deserialize<List<object>>(reader);
                            break;
                        default:
                            Throw("Unexpected token");
                            break;
                    }
                }

                // Return it
                return dict;
            }
            else if (objectType == typeof(List<object>))
            {
                // Get the list to populate
                List<object> list = existingValue as List<object>;
                if (list == null) list = new List<object>();

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
                            list.Add(reader.Value);
                            break;
                        case JsonToken.Integer:
                            list.Add(Convert.ToInt32(reader.Value));
                            break;
                        case JsonToken.StartObject:
                            list.Add(serializer.Deserialize<Dictionary<string, object>>(reader));
                            break;
                        case JsonToken.StartArray:
                            list.Add(serializer.Deserialize<List<object>>(reader));
                            break;
                        default:
                            Throw("Unexpected token");
                            break;
                    }
                }

                // Return it
                return list;
            }
            else
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
                Dictionary<string, object> dict = value as Dictionary<string, object>;

                // Start object
                writer.WriteStartObject();

                // Simply loop through and serialise
                foreach (var pair in dict)
                {
                    writer.WritePropertyName(pair.Key);
                    serializer.Serialize(writer, pair.Value);
                }

                // End object
                writer.WriteEndObject();
            }
            else if (value is List<object>)
            {
                // Get the list to write
                List<object> list = value as List<object>;

                // Start array
                writer.WriteStartArray();
                
                // Simply loop through and serialise
                for (int i = 0; i < list.Count; i++)
                    serializer.Serialize(writer, list[i]);

                // End array
                writer.WriteEndArray();
            }
        }
    }
}
