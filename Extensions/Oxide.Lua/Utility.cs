using NLua;
using Oxide.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Core.Lua
{
    /// <summary>
    /// Contains extension and utility methods
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Returns if the Lua table represents an array or not
        /// </summary>
        /// <param name="table"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool IsArray(this LuaTable table, out int count)
        {
            count = 0;
            foreach (var key in table.Keys)
            {
                if (!(key is double)) return false;
                var numkey = (double)key;
                if (Math.Floor(numkey) != numkey) return false;
                if (numkey < 1.0) return false;
                if (numkey > count) count = (int)numkey;
            }
            return true;
        }

        /// <summary>
        /// Returns if the Lua table represents an array or not
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public static bool IsArray(this LuaTable table)
        {
            int count;
            return IsArray(table, out count);
        }

        /// <summary>
        /// Copies and translates the contents of the specified table into the specified config file
        /// </summary>
        /// <param name="config"></param>
        /// <param name="table"></param>
        public static void SetConfigFromTable(DynamicConfigFile config, LuaTable table)
        {
            config.Clear();
            foreach (var key in table.Keys)
            {
                var keystr = key as string;
                if (keystr == null) continue;
                var value = TranslateLuaItemToConfigItem(table[key]);
                if (value != null) config[keystr] = value;
            }
        }

        /// <summary>
        /// Translates a single object from its Lua form to its C# form for use in a config file
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static object TranslateLuaItemToConfigItem(object item)
        {
            if (item is string) return item;
            if (item is double)
            {
                // If it's whole, return it as an int
                var number = (double)item;
                if (Math.Truncate(number) == number)
                    return (int)number;
                return (float)number;
            }
            if (item is bool) return item;
            if (item is LuaTable)
            {
                var table = item as LuaTable;
                int count;
                if (table.IsArray(out count))
                {
                    var list = new List<object>();
                    for (var i = 0; i < count; i++)
                    {
                        var luaobj = table[(double)(i + 1)];
                        list.Add(luaobj != null ? TranslateLuaItemToConfigItem(luaobj) : null);
                    }
                    return list;
                }
                var dict = new Dictionary<string, object>();
                foreach (var key in table.Keys)
                {
                    if (key is string)
                        dict.Add(key as string, TranslateLuaItemToConfigItem(table[key]));
                }
                return dict;
            }
            return null;
        }

        private static void CreateFullPath(string fullPath, LuaTable tbl, NLua.Lua lua)
        {
            var path = fullPath.Split('.');
            for (var i = 0; i < path.Length - 1; i++)
            {
                if (tbl[path[i]] != null) continue;
                lua.NewTable("tmp");
                var table = (LuaTable)lua["tmp"];
                tbl[path[i]] = table;
                lua["tmp"] = null;
                tbl = table;
            }
        }

        /// <summary>
        /// Copies and translates the contents of the specified config file into the specified table
        /// </summary>
        /// <param name="config"></param>
        /// <param name="lua"></param>
        /// <returns></returns>
        public static LuaTable TableFromConfig(DynamicConfigFile config, NLua.Lua lua)
        {
            // Make a table
            lua.NewTable("tmp");
            var tbl = lua["tmp"] as LuaTable;
            lua["tmp"] = null;

            // Loop each item in config
            foreach (var pair in config)
            {
                CreateFullPath(pair.Key, tbl, lua);
                // Translate and set on table
                tbl[pair.Key] = TranslateConfigItemToLuaItem(lua, pair.Value);
            }

            // Return
            return tbl;
        }

        /// <summary>
        /// Translates a single object from its C# form to its Lua form
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        private static object TranslateConfigItemToLuaItem(NLua.Lua lua, object item)
        {
            // Switch on the object type
            if (item is int || item is float || item is double)
                return Convert.ToDouble(item);
            else if (item is bool)
                return Convert.ToBoolean(item);
            else if (item is string)
                return item;
            else if (item is List<object>)
            {
                lua.NewTable("tmplist");
                var tbl = lua["tmplist"] as LuaTable;
                lua["tmplist"] = null;

                var list = item as List<object>;
                for (var i = 0; i < list.Count; i++)
                {
                    tbl[i + 1] = TranslateConfigItemToLuaItem(lua, list[i]);
                }

                return tbl;
            }
            else
            {
                if (item is Dictionary<string, object>)
                {
                    lua.NewTable("tmpdict");
                    var tbl = lua["tmpdict"] as LuaTable;
                    lua["tmpdict"] = null;

                    var dict = item as Dictionary<string, object>;
                    foreach (var pair in dict)
                    {
                        CreateFullPath(pair.Key, tbl, lua);
                        tbl[pair.Key] = TranslateConfigItemToLuaItem(lua, pair.Value);
                    }

                    return tbl;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the namespace of the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetNamespace(Type type) => type.Namespace ?? string.Empty;

        /// <summary>
        /// Gets the full type name of the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetTypeName(Type type)
        {
            if (type.IsNested)
                return GetTypeName(type.DeclaringType) + "+" + type.Name;
            if (type.IsGenericType)
                return type.Name.Substring(0, type.Name.IndexOf('`'));
            return type.Name;
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
                    if (type != null) yield return type;
            }
        }
    }
}
