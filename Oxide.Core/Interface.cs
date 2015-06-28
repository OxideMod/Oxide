using System;

namespace Oxide.Core
{
    /// <summary>
    /// The interface class through which patched DLLs interact with Oxide
    /// </summary>
    public static class Interface
    {
        /// <summary>
        /// Gets the main OxideMod instance
        /// </summary>
        public static OxideMod Oxide { get; private set; }

        /// <summary>
        /// Gets or sets the debug callback to use
        /// </summary>
        public static NativeDebugCallback DebugCallback { get; set; }

        /// <summary>
        /// Initializes Oxide
        /// </summary>
        public static void Initialize()
        {
            // Create if not already created
            if (Oxide == null)
            {
                Oxide = new OxideMod(DebugCallback);
                Oxide.Load();
            }
        }

        /// <summary>
        /// Calls the specified deprecated hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallDeprecatedHook(string hookname, params object[] args)
        {
            return Oxide.CallDeprecatedHook(hookname, args);
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallHook(string hookname, params object[] args)
        {
            // Call into Oxide core
            return Oxide?.CallHook(hookname, args);
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Call(string hookname, params object[] args)
        {
            return CallHook(hookname, args);
        }

        /// <summary>
        /// Calls the specified hook and converts the return value to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T Call<T>(string hookname, params object[] args)
        {
            return (T)Convert.ChangeType(CallHook(hookname, args), typeof(T));
        }

        /// <summary>
        /// Gets the Oxide mod
        /// </summary>
        /// <returns></returns>
        public static OxideMod GetMod()
        {
            return Oxide;
        }
    }
}
