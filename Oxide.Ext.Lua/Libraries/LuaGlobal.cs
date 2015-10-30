using System;

using NLua;

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
        /// Gets the Lua environment
        /// </summary>
        public NLua.Lua LuaEnvironment { get; }

        /// <summary>
        /// Gets the logger that this library writes to
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Initializes a new instance of the LuaGlobal library
        /// </summary>
        /// <param name="lua"></param>
        /// <param name="logger"></param>
        public LuaGlobal(NLua.Lua lua, Logger logger)
        {
            LuaEnvironment = lua;
            Logger = logger;
        }

        /// <summary>
        /// Prints a message
        /// </summary>
        /// <param name="args"></param>
        [LibraryFunction("print")]
        public void Print(params object[] args)
        {
            string message = "null";

            if(args.Length == 1) message = args[0].ToString();
            else if(args.Length > 1)
            {
                message = "";
                for(int i = 0; i <= args.Length; ++i)
                {
                    if(i > 0) message += "\t";
                    message += args[i].ToString();
                }
            }

            Logger.Write(LogType.Info, message);
        }
    }
}
