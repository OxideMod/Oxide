using System;

using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    public class CovalencePlugin : CSharpPlugin
    {
        new static readonly Covalence covalence = Interface.Oxide.GetLibrary<Covalence>();

        protected string game = covalence.Game;
        protected IServer server = covalence.Server;
        protected IPlayerManager players = covalence.Players;

        public CovalencePlugin()
        {
        }

        /// <summary>
        /// Print an info message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void Log(string format, params object[] args)
        {
            Interface.Oxide.LogInfo("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print a warning message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogWarning(string format, params object[] args)
        {
            Interface.Oxide.LogWarning("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }

        /// <summary>
        /// Print an error message using the oxide root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected void LogError(string format, params object[] args)
        {
            Interface.Oxide.LogError("[{0}] {1}", Title, args.Length > 0 ? string.Format(format, args) : format);
        }
    }
}
