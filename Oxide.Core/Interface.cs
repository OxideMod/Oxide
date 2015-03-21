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
        /// Initializes Oxide
        /// </summary>
        public static void Initialize()
        {
            // Create if not already created
            if (Oxide == null)
            {
                Oxide = new OxideMod();
                Oxide.Load();
            }
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
            return Oxide.CallHook(hookname, args);
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
