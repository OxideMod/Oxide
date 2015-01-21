using System;

namespace Oxide.Core
{
    /// <summary>
    /// The interface class through which patched DLLs interact with Oxide
    /// </summary>
    public static class Interface
    {
        // The main Oxide mod
        private static OxideMod oxide;

        /// <summary>
        /// Initialises Oxide
        /// </summary>
        public static void Initialise()
        {
            // Create if not already created
            if (oxide == null)
            {
                oxide = new OxideMod();
                oxide.Load();
            }
        }

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static object CallHook(string hookname, object[] args)
        {
            // Call into Oxide core
            return oxide.CallHook(hookname, args);
        }

        /// <summary>
        /// Gets the Oxide mod
        /// </summary>
        /// <returns></returns>
        public static OxideMod GetMod()
        {
            return oxide;
        }
    }
}
