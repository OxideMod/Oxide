using System;

namespace Oxide.Core
{
    /// <summary>
    /// The interface class through which patched DLLs interact with Oxide
    /// </summary>
    public static class Interface
    {
        /// <summary>
        /// Gets the main Oxide mod instance
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
            if (Oxide != null) return;
            Oxide = new OxideMod(DebugCallback);
            Oxide.Load();
        }

        /// <summary>
        /// Calls the specified deprecated hook
        /// </summary>
        /// <param name="oldHook"></param>
        /// <param name="newHook"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, params object[] args)
        {
            return Oxide.CallDeprecatedHook(oldHook, newHook, expireDate, args);
        }

        /// <summary>
        /// Calls the specified deprecated hook
        /// </summary>
        /// <param name="oldHook"></param>
        /// <param name="newHook"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallDeprecated(string oldHook, string newHook, DateTime expireDate, params object[] args)
        {
            return CallDeprecatedHook(oldHook, newHook, expireDate, args);
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallHook(string hook, params object[] args) => Oxide?.CallHook(hook, args);

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Call(string hook, params object[] args) => CallHook(hook, args);

        /// <summary>
        /// Calls the specified hook and converts the return value to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T Call<T>(string hook, params object[] args) => (T)Convert.ChangeType(CallHook(hook, args), typeof(T));

        /// <summary>
        /// Gets the Oxide mod
        /// </summary>
        /// <returns></returns>
        public static OxideMod GetMod() => Oxide;
    }
}
