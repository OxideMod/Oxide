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
            if (Oxide != null) return;
            Oxide = new OxideMod(DebugCallback);
            Oxide.Load();
        }

        /// <summary>
        /// Calls the specified deprecated hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="newname"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallDeprecatedHook(string hookname, string newname, DateTime expireDate, params object[] args) => Oxide.CallDeprecatedHook(hookname, newname, expireDate, args);

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallHook(string hookname, params object[] args) => Oxide?.CallHook(hookname, args);

        public static object CallHook(string hookname) => Oxide?.CallHook(hookname);
        public static object CallHook(string hookname, object arg0) => Oxide?.CallHook(hookname, arg0);
        public static object CallHook(string hookname, object arg0, object arg1) => Oxide?.CallHook(hookname, arg0, arg1);
        public static object CallHook(string hookname, object arg0, object arg1, object arg2) => Oxide?.CallHook(hookname, arg0, arg1, arg2);
        public static object CallHook(string hookname, object arg0, object arg1, object arg2, object arg3) => Oxide?.CallHook(hookname, arg0, arg1, arg2, arg3);
        public static object CallHook(string hookname, object arg0, object arg1, object arg2, object arg3, object arg4) => Oxide?.CallHook(hookname, arg0, arg1, arg2, arg3, arg4);
        public static object CallHook(string hookname, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5) => Oxide?.CallHook(hookname, arg0, arg1, arg2, arg3, arg4, arg5);
        public static object CallHook(string hookname, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) => Oxide?.CallHook(hookname, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object Call(string hookname, params object[] args) => Oxide?.CallHook(hookname, args);

        public static object Call(string hookname) => Oxide?.CallHook(hookname);
        public static object Call(string hookname, object arg0) => Oxide?.CallHook(hookname, arg0);
        public static object Call(string hookname, object arg0, object arg1) => Oxide?.CallHook(hookname, arg0, arg1);
        public static object Call(string hookname, object arg0, object arg1, object arg2) => Oxide?.CallHook(hookname, arg0, arg1, arg2);
        public static object Call(string hookname, object arg0, object arg1, object arg2, object arg3) => Oxide?.CallHook(hookname, arg0, arg1, arg2, arg3);
        public static object Call(string hookname, object arg0, object arg1, object arg2, object arg3, object arg4) => Oxide?.CallHook(hookname, arg0, arg1, arg2, arg3, arg4);
        public static object Call(string hookname, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5) => Oxide?.CallHook(hookname, arg0, arg1, arg2, arg3, arg4, arg5);
        public static object Call(string hookname, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6) => Oxide?.CallHook(hookname, arg0, arg1, arg2, arg3, arg4, arg5, arg6);

        /// <summary>
        /// Calls the specified hook and converts the return value to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static T Call<T>(string hookname, params object[] args) => (T)Convert.ChangeType(CallHook(hookname, args), typeof(T));

        /// <summary>
        /// Gets the Oxide mod
        /// </summary>
        /// <returns></returns>
        public static OxideMod GetMod() => Oxide;
    }
}
