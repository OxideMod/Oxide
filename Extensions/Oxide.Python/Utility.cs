using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Oxide.Core.Python
{
    /// <summary>
    /// Contains extension and utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Copies and translates the contents of the specified dictionary into the specified config file
        /// </summary>
        /// <param name="config"></param>
        /// <param name="dict"></param>
        public static void SetConfigFromDictionary(DynamicConfigFile config, PythonDictionary dict)
        {
            config.Clear();
            foreach (object key in dict.Keys)
            {
                string keystr = key as string;
                if (keystr != null)
                {
                    object value = TranslatePythonItemToConfigItem(dict[key]);
                    if (value != null) config[keystr] = value;
                }
            }
        }

        /// <summary>
        /// Translates a single object from its Python form to its C# form for use in a config file
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static object TranslatePythonItemToConfigItem(object item)
        {
            if (item is string || item is bool || item is int || item is double || item is float)
            {
                return item;
            }
            var tuple = item as PythonTuple;
            if (tuple != null)
            {
                return tuple.Select(TranslatePythonItemToConfigItem).ToList();
            }
            var dictionary = item as PythonDictionary;
            if (dictionary != null)
            {
                Dictionary<string, object> dict = new Dictionary<string, object>();
                foreach (object key in dictionary.Keys)
                {
                    var s = key as string;
                    if (s != null)
                        dict.Add(s, TranslatePythonItemToConfigItem(dictionary[key]));
                }
                return dict;
            }
            return null;
        }

        /// <summary>
        /// Copies and translates the contents of the specified config file into the specified dictionary
        /// </summary>
        /// <param name="config"></param>
        /// <param name="engine"></param>
        /// <returns></returns>
        public static PythonDictionary DictionaryFromConfig(DynamicConfigFile config, ScriptEngine engine)
        {
            PythonDictionary tbl = new PythonDictionary();
            // Loop each item in config
            foreach (var pair in config)
            {
                // Translate and set on table
                tbl[pair.Key] = TranslateConfigItemToPythonItem(pair.Value);
            }

            // Return
            return tbl;
        }

        /// <summary>
        /// Translates a single object from its C# form to its Lua form
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static object TranslateConfigItemToPythonItem(object item)
        {
            // Switch on the object type
            if (item is int || item is float || item is double || item is bool || item is string)
                return item;
            var objects = item as List<object>;
            if (objects != null)
            {
                return new PythonTuple(objects);
            }
            var dictionary = item as Dictionary<string, object>;
            if (dictionary != null)
            {
                PythonDictionary tbl = new PythonDictionary();
                Dictionary<string, object> dict = dictionary;
                foreach (var pair in dict)
                {
                    tbl[pair.Key] = TranslateConfigItemToPythonItem(pair.Value);
                }

                return tbl;
            }
            return null;
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

        /// <summary>
        /// Gets all types of an assembly
        /// </summary>
        /// <param name="asm"></param>
        /// <returns></returns>
        public static IEnumerable<Type> GetAllTypesFromAssembly(Assembly asm)
        {
            foreach (var module in asm.GetModules())
            {
                Type[] moduleTypes;
                try
                {
                    moduleTypes = module.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    moduleTypes = e.Types;
                }
                catch (Exception)
                {
                    moduleTypes = new Type[0];
                }

                foreach (var type in moduleTypes)
                {
                    if (type != null)
                    {
                        yield return type;
                    }
                }
            }
        }
    }
}
