using System;

using Jint;
using Jint.Native;
using Jint.Native.Object;

using Oxide.Core.Configuration;

namespace Oxide.Ext.JavaScript
{
    /// <summary>
    /// Contains extension and utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Copies and translates the contents of the specified table into the specified config file
        /// </summary>
        /// <param name="config"></param>
        /// <param name="objectInstance"></param>
        public static void SetConfigFromObject(DynamicConfigFile config, ObjectInstance objectInstance)
        {
            config.Clear();
            foreach (var property in objectInstance.Properties)
            {
                if (property.Value.Value == null) continue;
                object value = property.Value.Value.Value.ToObject();
                if (value != null) config[property.Key] = value;
            }
        }

        /// <summary>
        /// Copies and translates the contents of the specified config file into the specified object
        /// </summary>
        /// <param name="config"></param>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static ObjectInstance ObjectFromConfig(DynamicConfigFile config, Engine engine)
        {
            var tbl = new ObjectInstance(engine) {Extensible = true};
            // Loop each item in config
            foreach (var pair in config)
            {
                // Translate and set on object
                tbl.FastAddProperty(pair.Key, JsValue.FromObject(engine, pair.Value), true, false, true);
            }

            // Return
            return tbl;
        }

        /// <summary>
        /// Gets the namespace of the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetNamespace(Type type)
        {
            return type.Namespace ?? string.Empty;
        }
    }
}
