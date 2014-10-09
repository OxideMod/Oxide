using System;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Core.Logging
{
    /// <summary>
    /// Represents a set of loggers that fall under a single logger
    /// </summary>
    public sealed class CompoundLogger : Logger
    {
        // Loggers under this compound logger
        private HashSet<Logger> subloggers;

        // Any cached messages for new loggers
        private List<LogMessage> messagecache;

        /// <summary>
        /// Initialises a new instance of the CompoundLogger class
        /// </summary>
        /// <param name="loggers"></param>
        public CompoundLogger()
            : base(true)
        {
            // Initialise
            subloggers = new HashSet<Logger>();
            messagecache = new List<LogMessage>();
        }

        /// <summary>
        /// Adds a sublogger to this compound logger
        /// </summary>
        /// <param name="logger"></param>
        public void AddLogger(Logger logger)
        {
            // Register it
            subloggers.Add(logger);

            // Write the message cache to it
            for (int i = 0; i < messagecache.Count; i++)
            {
                logger.Write(messagecache[i]);
            }
        }

        /// <summary>
        /// Removes a sublogger from this compound logger
        /// </summary>
        /// <param name="logger"></param>
        public void RemoveLogger(Logger logger)
        {
            // Unregister it
            subloggers.Remove(logger);
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public override void Write(LogType type, string format, params object[] args)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
                logger.Write(type, format, args);

            // Cache it for any loggers added late
            messagecache.Add(CreateLogMessage(type, format, args));
        }
    }
}
