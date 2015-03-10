﻿using System;
using System.Collections.Generic;

using Jint;
using Jint.Native;
using Jint.Native.Array;
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
            var objInst = new ObjectInstance(engine) {Extensible = true};
            foreach (var pair in config)
            {
                objInst.FastAddProperty(pair.Key, JsValueFromObject(pair.Value, engine), true, true, true);
            }
            return objInst;
        }

        public static JsValue JsValueFromObject(object obj, Engine engine)
        {
            var values = obj as List<object>;
            if (values != null)
            {
                var jsValues = new List<JsValue>();
                foreach (var v in values)
                    jsValues.Add(JsValueFromObject(v, engine));
                var array = (ArrayInstance) engine.Array.Construct(jsValues.ToArray());
                array.Extensible = true;
                return array;
            }
            var dict = obj as Dictionary<string, object>;
            if (dict != null)
            {
                var objInst = new ObjectInstance(engine) { Extensible = true };
                foreach (var pair in dict)
                {
                    objInst.FastAddProperty(pair.Key, JsValueFromObject(pair.Value, engine), true, true, true);
                }
                return objInst;
            }
            return JsValue.FromObject(engine, obj);
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
