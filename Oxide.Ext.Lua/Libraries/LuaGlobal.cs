using Oxide.Core.Libraries;
using Oxide.Core.Logging;

namespace Oxide.Ext.Lua.Libraries
{
    /// <summary>
    /// A global library containing game-agnostic Lua utilities
    /// </summary>
    public class LuaGlobal : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => true;

        /// <summary>
        /// Gets the logger that this library writes to
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the LuaGlobal library
        /// </summary>
        /// <param name="logger"></param>
        public LuaGlobal(Logger logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Prints a message
        /// </summary>
        /// <param name="message"></param>
        [LibraryFunction("print")]
        public void Print(object message)
        {
            Logger.Write(LogType.Info, message != null ? message.ToString() : "null");
        }
    }
}
